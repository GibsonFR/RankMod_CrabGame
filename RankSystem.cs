using static RankMod.RankSystemConstants;
using static RankMod.RankSystemUtility;

namespace RankMod
{
    public class RankSystemConstants
    {
    }

    public class RankSystemManager : MonoBehaviour
    {
        /// <summary>
        /// Updates the ranking system state during the game loop.
        /// Handles Elo updates and player tracking based on game events.
        /// </summary>
        void Update()
        {
            if (!ranked || !IsHost()) return;

            if (GetGameState() == "Playing")
            {
                if (!gameHasStarted)
                {
                    SetGameVariables();

                    foreach (var player in playersInRanked)
                    {
                        var playerData = Database._instance.GetPlayerData(player);
                        if (playerData != null &&
                            playerData.Properties.TryGetValue("GamesPlayed", out object gamesPlayedObj))
                        {
                            int gamesPlayed = Convert.ToInt32(gamesPlayedObj);
                            Database._instance.SetData(player, "GamesPlayed", gamesPlayed + 1);
                        }
                    }

                    foreach (var player in connectedPlayers)
                    {
                        SendPrivateMessage(player.Key, $"[RankMod] Avg elo : {(int)averageGameElo} ({RankName.GetRankFromElo(averageGameElo)}) | Players : {playersThisGame}");
                    }
                }
            }

            if (GetGameState() == "GameOver" && gameHasStarted)
            {
                List<ulong> playersAliveList = GetPlayerAliveList();
                foreach (var player in playersAliveList)
                {
                    try
                    {
                        UpdateElo(player, playersAliveList.Count > 1);

                        var playerData = Database._instance.GetPlayerData(player);
                        if (playerData != null &&
                            playerData.Properties.TryGetValue("Wins", out object winsObj))
                        {
                            int wins = Convert.ToInt32(winsObj);
                            Database._instance.SetData(player, "Wins", wins + 1);
                        }

                        playersInRanked.Remove(player);
                    }
                    catch (Exception ex)
                    {
                        Log(logFilePath, $"Error updating Elo for player {player}: {ex.Message}");
                    }
                }

                ResetGameVariables();
            }
        }
    }


    public class RankSystemPatches
    {
        /// <summary>
        /// Handles Elo updates when a player dies in a ranked game.
        /// </summary>
        [HarmonyPatch(typeof(ServerSend), nameof(ServerSend.PlayerDied))]
        [HarmonyPrefix]
        public static void OnServerSendPlayerDiedPre(ref ulong __0, ref ulong __1)
        {
            if (!gameHasStarted || !IsHost() || !ranked) return;

            UpdateElo(__0, false);
            playersInRanked.Remove(__0);
        }

        /// <summary>
        /// Handles Elo updates when a player leaves the lobby mid-game.
        /// </summary>
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
        /// <summary>
        /// Calculates the average Elo of all players in the current game.
        /// </summary>
        public static float GetAverageGameElo() => playersThisGame > 0 ? CalculateTotalElo() / playersThisGame : 1000;

        /// <summary>
        /// Resets game-related variables at the end of a match.
        /// </summary>
        public static void ResetGameVariables()
        {
            gameHasStarted = false;
            playersThisGame = 0;
            playersInRanked.Clear();
            averageGameElo = 0;
            totalGameExpectative = 0;
        }

        /// <summary>
        /// Initializes game-related variables at the start of a match.
        /// </summary>
        public static void SetGameVariables()
        {
            gameHasStarted = true;
            List<ulong> alivePlayers = GetPlayerAliveList();
            playersThisGame = alivePlayers.Count;
            playersInRanked = alivePlayers;
            averageGameElo = GetAverageGameElo();
            totalGameExpectative = GetTotalGameExpectative();
        }

        /// <summary>
        /// Calculates the total Elo of all currently alive players.
        /// </summary>
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

        /// <summary>
        /// Calculates the total expected win probability for all players based on their Elo.
        /// </summary>
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

        /// <summary>
        /// Calculates the probability of a player winning based on their Elo compared to the average game Elo.
        /// </summary>
        public static float WinExpectative(float playerElo, float averageGameElo)
            => 1.0f / (1.0f + (float)Math.Pow(10.0, (averageGameElo - playerElo) / eloScalingFactor));

        /// <summary>
        /// Calculates the penalty percentage based on the player's ranking position in the game.
        /// </summary>
        public static float GetMalusPercent(float rank)
            => ((rank - 1f) / (playersThisGame - 1f)) / (playersThisGame / 2f);

        /// <summary>
        /// Calculates the malus (penalty) applied to Elo gain based on game expectations.
        /// </summary>
        public static float GetMalus()
            => kFactor * ((playersThisGame - 2f) - totalGameExpectative) * -1f;

        /// <summary>
        /// Updates a player's Elo based on their ranking and whether they shared the rank with others.
        /// </summary>
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

            float rank = sharedRank ? ((float)alivePlayers + 1f) / 2f : alivePlayers;
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

            /// <summary>
            /// Retrieves the player's rank based on their Elo rating.
            /// </summary>
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
