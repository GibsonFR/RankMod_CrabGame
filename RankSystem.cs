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

                    foreach (var player in connectedPlayers) SendPrivateMessage(player.Key, $"[RankMod] Avg elo : {(int)averageGameElo} ({RankName.GetRankFromElo(averageGameElo)}) | Players : {playersThisGame}");
                    
                }
            }

            if (GetGameState() == "GameOver" && gameHasStarted)
            {
                List<ulong> playersAliveList = GetPlayerAliveList();
                foreach (var player in playersAliveList)
                {
                    try
                    {
                        if (playersAliveList.Count > 1) UpdateElo(player, true);                    
                        else UpdateElo(player, false);

                        playersInRanked.Remove(player);
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

            UpdateElo(__0, false);
            playersInRanked.Remove(__0);
        }

        [HarmonyPatch(typeof(LobbyManager), nameof(LobbyManager.RemovePlayerFromLobby))]
        [HarmonyPrefix]
        public static void OnLobbyManagerRemovePlayerFromLobbyPre(CSteamID __0)
        {
            if (!gameHasStarted || !playersInRanked.Contains((ulong)__0) || !IsHost() || !ranked) return;


            UpdateElo((ulong)__0, false);
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
                .Select(id =>
                {
                    var playerData = Database._instance?.GetPlayerData(id);

                    // Ensure Elo is retrieved properly from the Properties dictionary
                    if (playerData?.Properties.TryGetValue("Elo", out object storedElo) == true && storedElo is float eloValue)
                    {
                        return eloValue;
                    }

                    return 1000f; // Default fallback
                })
                .Sum();
        }

        public static float GetTotalGameExpectative()
        {
            return GetPlayerAliveList()
                .Select(id =>
                {
                    var playerData = Database._instance?.GetPlayerData(id);
                    float playerElo = 1000f; // Default value

                    // Retrieve Elo if available
                    if (playerData?.Properties.TryGetValue("Elo", out object storedElo) == true && storedElo is float eloValue)
                    {
                        playerElo = eloValue;
                    }

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

        public static void UpdateElo(ulong steamId, bool sharedRank)
        {
            var playerData = Database._instance?.GetPlayerData(steamId);

            if (playerData == null)
            {
                SendPrivateMessage(steamId, "[RankMod] Player data not found.");
                return;
            }

            float playerElo = playerData.Properties.TryGetValue("Elo", out var storedElo) && storedElo is float elo
                ? elo
                : (playerData.Properties.TryGetValue("LastElo", out var storedLastElo) && storedLastElo is float lastElo ? lastElo : 1000f);

            Database._instance.SetData(steamId, "LastElo", playerElo);

            float malus = GetMalus();
            int alivePlayers = GetPlayerAliveList().Count;

            float rank;
            if (!sharedRank) rank = alivePlayers;
            else rank = ((float)alivePlayers + 1f) / 2f;

            float malusPercent = GetMalusPercent(rank);
            

            float eloGain = kFactor * (1 - (malusPercent * 2) - WinExpectative(playerElo, averageGameElo));
            eloGain += malus * malusPercent;

            playerElo = Math.Max(100, playerElo + eloGain);

            string sign = eloGain >= 0 ? "+" : "";
            SendPrivateMessage(steamId, $"[top{rank}/{playersThisGame}] [{sign}{eloGain:F1}] --> Your Elo: {playerElo:F1} ({RankName.GetRankFromElo(playerElo)})");

            // Update Elo in database
            Database._instance.SetData(steamId, "Elo", playerElo);
            Database._instance.SetData(steamId, "Rank", RankName.GetRankFromElo(playerElo));
        }


        public static class RankName
        {
            private static readonly SortedDictionary<int, string> EloRanks = new()
            {
                { 100, "Clown" },
                { 800, "Bronze I" },
                { 850, "Bronze II" },
                { 900, "Bronze III" },
                { 950, "Bronze IV" },
                { 975, "Silver I" },
                { 1000, "Silver II" },
                { 1025, "Silver III" },
                { 1050, "Silver IV" },
                { 1075, "Gold I" },
                { 1100, "Gold II" },
                { 1125, "Gold III" },
                { 1150, "Gold IV" },
                { 1170, "Platinum I" },
                { 1190, "Platinum II" },
                { 1210, "Platinum III" },
                { 1230, "Platinum IV" },
                { 1245, "Diamond I" },
                { 1260, "Diamond II" },
                { 1275, "Diamond III" },
                { 1290, "Diamond IV" },
                { 1300, "Master I" },
                { 1310, "Master II" },
                { 1320, "Master III" },
                { 1330, "Master IV" },
                { 1350, "GM I" },
                { 1370, "GM II" },
                { 1390, "GM III" },
                { 1400, "GM IV" },
                { 2000, "Challenger" },
            };

            public static string GetRankFromElo(float elo)
            {
                string rank = "Unranked"; // Default rank if below the lowest range

                foreach (var entry in EloRanks)
                {
                    if (elo >= entry.Key)
                        rank = entry.Value;
                    else
                        break; // Stop checking once we exceed the current Elo range
                }

                return rank;
            }
        }

    }
}
