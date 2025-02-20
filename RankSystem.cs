using static RankMod.RankSystemConstants;
using static RankMod.RankSystemUtility;

namespace RankMod
{
    public class RankSystemConstants
    {
    }

    public class RankSystemManager : MonoBehaviour
    {
        void Update()
        {
            if (!ranked || !IsHost()) return;

            if (GetGameState() == "Playing")
            {
                if (!gameHasStarted)
                {
                    SetGameVariables();
                    SendServerMessage($"[RankMod] Average elo : {(int)averageGameElo} | Players : {playersThisGame}");
                }
            }

            if (GetGameState() == "GameOver" && gameHasStarted)
            {
                foreach (var player in GameManager.Instance.activePlayers)
                {
                    try
                    {
                        if (IsPlayerInactive(player)) continue;

                        ulong playerId = player.value.steamProfile.m_SteamID;
                        UpdateElo(playerId, 0);
                        playersInRanked.Remove(playerId);
                        break;
                    }
                    catch { }
                }

                ResetGameVariables();
            }



        }
    }

    public class RankSystemPatches
    {
        [HarmonyPatch(typeof(ServerSend), nameof(ServerSend.PlayerDied))]
        [HarmonyPrefix]
        public static void OnServerSendPlayerDiedPre(ref ulong __0, ref ulong __1)
        {
            if (!gameHasStarted || !IsHost() || !ranked) return;

            UpdateElo(__0, 0);
            playersInRanked.Remove(__0);
        }

        [HarmonyPatch(typeof(LobbyManager), nameof(LobbyManager.RemovePlayerFromLobby))]
        [HarmonyPrefix]
        public static void OnLobbyManagerRemovePlayerFromLobbyPre(CSteamID __0)
        {
            if (!gameHasStarted || !playersInRanked.Contains((ulong)__0) || !IsHost() || !ranked) return;


            UpdateElo((ulong)__0, 0);
            playersInRanked.Remove((ulong)__0);
        }
    }

    public class RankSystemUtility
    {
        public static float GetAverageGameElo() => playersThisGame > 0 ? CalculateTotalElo() / playersThisGame : 1000;
        public static bool IsPlayerInactive(Il2CppSystem.Collections.Generic.KeyValuePair<ulong, PlayerManager> player) => player.value.dead || player.value.transform.position == Vector3.zero;

        public static void ResetGameVariables()
        {
            gameHasStarted = false;
            playersThisGame = 0;
            playersInRanked.Clear();
            averageGameElo = 0;
            totalGameExpectative = 0;
        }

        public static void SetGameVariables()
        {
            gameHasStarted = true;
            List<ulong> alivePlayers = GetPlayerAliveList();
            playersThisGame = alivePlayers.Count;
            playersInRanked = alivePlayers;
            averageGameElo = GetAverageGameElo();
            totalGameExpectative = GetTotalGameExpectative();
        }

        public static float CalculateTotalElo()
        {
            return GetPlayerAliveList()
                .Select(id => Database._instance?.GetPlayerData(id)?.Elo ?? 1000)
                .Sum();
        }

        public static float GetTotalGameExpectative()
        {
            return GetPlayerAliveList()
                .Select(id =>
                {
                    float playerElo = Database._instance?.GetPlayerData(id)?.Elo ?? 1000;
                    return WinExpectative(playerElo, averageGameElo);
                })
                .Sum();
        }

        public static float WinExpectative(float playerElo, float averageGameElo) => 1.0f / (1.0f + (float)Math.Pow(10.0, (averageGameElo - playerElo) / eloScalingFactor));
        public static float GetMalusPercent(float rank) => ((rank - (float)1) / ((float)playersThisGame - (float)1)) / ((float)playersThisGame / (float)2);
        public static float GetMalus() => (float)kFactor * (((float)playersThisGame - (float)2) - (float)totalGameExpectative) * (float)-1;

        public static List<ulong> GetPlayerAliveList()
        {
            return connectedPlayers
                .Where(player => player.Value != null
                              && !player.Value.dead
                              && player.Value.transform.position != Vector3.zero)
                .Select(player => player.Value.steamProfile.m_SteamID)
                .ToList();
        }

        public static void UpdateElo(ulong steamId, int rankBonus)
        {
            float playerElo = Database._instance?.GetPlayerData(steamId)?.Elo ?? 1000;

            float malus = GetMalus();

            int alivePlayers = GetPlayerAliveList().Count;

            float rank = alivePlayers + rankBonus;
            if (rank == 0) return;

            float malusPercent = GetMalusPercent(rank);

            float eloGain = kFactor * (1 - (malusPercent * 2) - WinExpectative(playerElo, averageGameElo));
            eloGain += malus * malusPercent;

            playerElo = Math.Max(100, playerElo + eloGain); 

            string sign = eloGain >= 0 ? "+" : "";
            SendPrivateMessage(steamId, $"[{sign}{eloGain:F1}] --> Your Elo: {playerElo:F1}");

            Database._instance.SetData(steamId, "Elo", playerElo);
        }

    }
}
