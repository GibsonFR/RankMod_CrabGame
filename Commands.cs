using static RankMod.CommandUtility;

namespace RankMod
{
    public class CommandPatches
    {
        /// <summary>
        /// Intercepts chat messages and processes commands if detected.
        /// </summary>
        [HarmonyPatch(typeof(ServerSend), nameof(ServerSend.SendChatMessage))]
        [HarmonyPrefix]
        public static bool ServerSendSendChatMessagePre(ulong __0, string __1)
        {
            if (!IsHost()) return true;

            if (IsCommand(__1))
            {
                string[] parts = __1.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0) return false;

                ExecuteCommand(parts[0], parts, __0, __0 == clientId);
                return false;
            }
            return true;
        }
    }

    public class CommandUtility
    {
        /// <summary>
        /// Executes the corresponding command based on user input.
        /// </summary>
        public static void ExecuteCommand(string command, string[] arguments, ulong senderId, bool isAdmin)
        {
            var adminCommands = new Dictionary<string, Action<string[], ulong>>
            {
                { $"{commandSymbol}ranked", HandleRankedCommand },
                { $"{commandSymbol}lastElo", HandleLastEloCommand }
            };

                    var playerCommands = new Dictionary<string, Action<string[], ulong>>
            {
                { $"{commandSymbol}elo", HandleEloCommand },
                { $"{commandSymbol}rank", HandleEloCommand },
                { $"{commandSymbol}help", HandleHelpCommand },
                { $"{commandSymbol}dev", HandleDevCommand },
                { $"{commandSymbol}leaderboard", HandleLeaderboardCommand },
                { $"{commandSymbol}win", HandleWinCommand },
            };

            if (isAdmin && adminCommands.TryGetValue(command, out var adminAction))
            {
                adminAction(arguments, senderId);
            }
            else if (playerCommands.TryGetValue(command, out var playerAction))
            {
                playerAction(arguments, senderId);
            }
            else
            {
                SendPrivateMessage(senderId, $"Unknown command! Try {commandSymbol}help for a list of commands.");
            }
        }

        /// <summary>
        /// Checks if a given message is a command.
        /// </summary>
        public static bool IsCommand(string msg) =>
            !string.IsNullOrEmpty(msg) && msg.StartsWith(commandSymbol);


        /// <summary>
        /// Toggles ranked mode on or off.
        /// </summary>
        public static void HandleRankedCommand(string[] arguments, ulong senderId)
        {
            ranked = !ranked;
            string statusMessage = ranked ? "[RankMod] Games are now ranked" : "[RankMod] Games aren't ranked anymore";
            SendPrivateMessage(senderId, statusMessage);
        }

        /// <summary>
        /// Finds a player's SteamID based on a player number or username.
        /// </summary>
        public static ulong? CommandPlayerFinder(string identifier)
        {
            if (identifier.StartsWith("#") && int.TryParse(identifier.AsSpan(1), out int playerNumber))
            {
                return GetPlayerSteamId(playerNumber);
            }

            return GetPlayerSteamId(identifier);
        }

        /// <summary>
        /// Retrieves the SteamID of a player based on their in-game player number.
        /// </summary>
        public static ulong? GetPlayerSteamId(int playerNumber)
        {
            return connectedPlayers.Values
                .FirstOrDefault(player => player != null && player.playerNumber == playerNumber)
                ?.steamProfile.m_SteamID;
        }

        /// <summary>
        /// Retrieves the SteamID of a player based on their username.
        /// Supports both exact and partial (substring) matches.
        /// </summary>
        public static ulong? GetPlayerSteamId(string username)
        {
            var players = Database._instance.GetAllPlayers();

            // Try exact match first
            var exactMatch = players.FirstOrDefault(player =>
                player.Properties.TryGetValue("Username", out var storedUsername) &&
                storedUsername is string storedName &&
                storedName.Equals(username, StringComparison.OrdinalIgnoreCase));

            if (exactMatch != null)
                return exactMatch.ClientId;

            // Try partial match (contains)
            var partialMatch = players.FirstOrDefault(player =>
                player.Properties.TryGetValue("Username", out var storedUsername) &&
                storedUsername is string storedName &&
                storedName.IndexOf(username, StringComparison.OrdinalIgnoreCase) >= 0);

            return partialMatch?.ClientId;
        }

        /// <summary>
        /// Handles the .elo command, allowing players to check their own or another player's Elo.
        /// </summary>
        private static void HandleEloCommand(string[] arguments, ulong senderId)
        {
            if (arguments.Length > 2)
            {
                SendInvalidCommandMessage(senderId, $"{commandSymbol}elo | {commandSymbol}elo #playerNumber | {commandSymbol}elo playerName");
                return;
            }

            ulong playerId = (arguments.Length == 2) ? CommandPlayerFinder(arguments[1]) ?? 0 : senderId;

            if (playerId == 0)
            {
                SendPrivateMessage(senderId, "Player not found!");
                return;
            }

            SendEloMessage(senderId, playerId);
        }

        /// <summary>
        /// Sends a private message to display the Elo and rank of a player.
        /// </summary>
        private static void SendEloMessage(ulong recipientId, ulong playerId)
        {
            PlayerData playerData = Database._instance?.GetPlayerData(playerId);

            if (playerData != null)
            {
                // Retrieve Username, Rank, and Elo dynamically
                string username = playerData.Properties.TryGetValue("Username", out var storedUsername) && storedUsername is string name
                    ? name
                    : "Unknown";

                string rank = playerData.Properties.TryGetValue("Rank", out var storedRank) && storedRank is string rankName
                    ? rankName
                    : "Unranked";

                float elo = playerData.Properties.TryGetValue("Elo", out var storedElo) && storedElo is float eloValue
                    ? eloValue
                    : 1000f;

                // Construct the message
                string message = (recipientId == playerId)
                    ? $"[{rank}] {username}, your Elo is: {elo}"
                    : $"[{rank}] {username} Elo: {elo}";

                SendPrivateMessage(recipientId, message);
            }
            else
            {
                SendPrivateMessage(recipientId, "Player data not found!");
            }
        }

        /// <summary>
        /// Handles the command to restore the last recorded Elo of a player.
        /// </summary>
        private static void HandleLastEloCommand(string[] arguments, ulong senderId)
        {
            if (arguments.Length > 2)
            {
                SendInvalidCommandMessage(senderId, $"{commandSymbol}lastElo | {commandSymbol}lastElo #playerNumber | {commandSymbol}lastElo *");
                return;
            }

            if (arguments.Length == 1)
            {
                // Restore the sender's last Elo
                RestoreLastElo(senderId);
            }
            else if (arguments[1] == "*")
            {
                // Restore last Elo for all connected players
                foreach (var player in connectedPlayers.Keys)
                {
                    RestoreLastElo(player);
                }
                SendPrivateMessage(senderId, "[RankMod] LastElo has been restored for all players.");
            }
            else
            {
                // Find the player by ID or username and restore their LastElo
                ulong targetPlayerId = CommandPlayerFinder(arguments[1]) ?? 0;

                if (targetPlayerId == 0)
                {
                    SendPrivateMessage(senderId, "Player not found!");
                    return;
                }

                RestoreLastElo(targetPlayerId);
                SendPrivateMessage(senderId, $"[RankMod] LastElo has been restored for {arguments[1]}.");
            }
        }

        /// <summary>
        /// Restores the last recorded Elo of a player.
        /// </summary>
        private static void RestoreLastElo(ulong playerId)
        {
            PlayerData playerData = Database._instance?.GetPlayerData(playerId);

            if (playerData != null)
            {
                // Retrieve LastElo dynamically from the Properties dictionary
                float lastElo = playerData.Properties.TryGetValue("LastElo", out var storedLastElo) && storedLastElo is float lastEloValue
                    ? lastEloValue
                    : 1000f; // Default fallback value if LastElo is missing

                // Update Elo with LastElo
                Database._instance.SetData(playerId, "Elo", lastElo);
                SendPrivateMessage(playerId, $"[RankMod] Your Elo has been restored to {lastElo}.");
            }
            else
            {
                SendPrivateMessage(playerId, "[RankMod] No LastElo data available.");
            }
        }

        /// <summary>
        /// Handles the leaderboard command, which displays player rankings.
        /// Supports checking individual player ranks or showing the top players.
        /// </summary>
        private static void HandleLeaderboardCommand(string[] arguments, ulong senderId)
        {
            if (arguments.Length == 1)
            {
                SendPlayerRank(senderId, senderId);
            }
            else if (arguments.Length == 2)
            {
                if (arguments[1] == "top")
                {
                    SendTop5Leaderboard(senderId);
                }
                else
                {
                    ulong targetPlayerId = CommandPlayerFinder(arguments[1]) ?? 0;
                    if (targetPlayerId == 0)
                    {
                        SendPrivateMessage(senderId, "Player not found!");
                        return;
                    }
                    SendPlayerRank(senderId, targetPlayerId);
                }
            }
            else
            {
                SendInvalidCommandMessage(senderId, $"{commandSymbol}leaderboard | {commandSymbol}leaderboard player | {commandSymbol}leaderboard top");
            }
        }

        /// <summary>
        /// Sends the rank of a specific player within the leaderboard.
        /// </summary>
        private static void SendPlayerRank(ulong recipientId, ulong playerId)
        {
            var leaderboard = Database._instance?.GetAllPlayers()
                .Where(p => p.Properties.TryGetValue("Elo", out var eloValue) && eloValue is float)
                .OrderByDescending(p => (float)p.Properties["Elo"])
                .Select((player, index) => new { player.ClientId, Rank = index + 1 })
                .ToList();

            var playerEntry = leaderboard?.FirstOrDefault(p => p.ClientId == playerId);

            if (playerEntry != null)
            {
                SendPrivateMessage(
                    recipientId,
                    $"[{Database._instance?.GetPlayerData(playerId).Properties["Rank"]}] " +
                    $"{Database._instance?.GetPlayerData(playerId).Properties["Username"]} " +
                    $"leaderboard rank: {playerEntry.Rank}/{leaderboard.Count}"
                );
            }
            else
            {
                SendPrivateMessage(recipientId, "Player not found in the leaderboard.");
            }
        }

        /// <summary>
        /// Sends the top 5 players in the leaderboard based on Elo ranking.
        /// </summary>
        private static void SendTop5Leaderboard(ulong recipientId)
        {
            var leaderboard = Database._instance?.GetAllPlayers()
                .Where(p => p.Properties.TryGetValue("Elo", out var eloValue) && eloValue is float)
                .OrderByDescending(p => (float)p.Properties["Elo"])
                .Take(5)
                .Select((player, index) => new
                {
                    Top = index + 1,
                    Rank = player.Properties["Rank"],
                    Username = player.Properties["Username"],
                    Elo = player.Properties["Elo"]
                })
                .ToList();

            if (leaderboard != null && leaderboard.Count > 0)
            {
                SendPrivateMessage(recipientId, "[RankMod] Top 5 Leaderboard:");
                foreach (var entry in leaderboard)
                {
                    SendPrivateMessage(
                        recipientId,
                        $"top{entry.Top} [{entry.Rank}] {entry.Username}: {entry.Elo:F1} Elo"
                    );
                }
            }
            else
            {
                SendPrivateMessage(recipientId, "No leaderboard data available.");
            }
        }

        /// <summary>
        /// Handles the `.win` command, which displays the player's total wins and win rate.
        /// </summary>
        private static void HandleWinCommand(string[] arguments, ulong senderId)
        {
            if (arguments.Length > 2)
            {
                SendInvalidCommandMessage(senderId, $"{commandSymbol}win | {commandSymbol}win player");
                return;
            }

            ulong playerId = (arguments.Length == 2) ? CommandPlayerFinder(arguments[1]) ?? 0 : senderId;

            if (playerId == 0)
            {
                SendPrivateMessage(senderId, "Player not found!");
                return;
            }

            SendWinMessage(senderId, playerId);
        }

        /// <summary>
        /// Sends the player's win statistics (total wins and win rate).
        /// </summary>
        private static void SendWinMessage(ulong recipientId, ulong playerId)
        {
            PlayerData playerData = Database._instance?.GetPlayerData(playerId);

            if (playerData != null)
            {
                // Retrieve Username dynamically
                string username = playerData.Properties.TryGetValue("Username", out var storedUsername) && storedUsername is string name
                    ? name
                    : "Unknown";

                // Retrieve Wins and GamesPlayed dynamically
                int gamesPlayed = playerData.Properties.TryGetValue("GamesPlayed", out var storedGames) && storedGames is int totalGames
                    ? totalGames
                    : 0;

                int wins = playerData.Properties.TryGetValue("Wins", out var storedWins) && storedWins is int totalWins
                    ? totalWins
                    : 0;

                float winRate = (gamesPlayed > 0) ? (wins / (float)gamesPlayed) * 100 : 0f;

                // Construct the message
                string message = (recipientId == playerId)
                    ? $"[RankMod] {username}, Wins: {wins} | Games Played: {gamesPlayed} | Win Rate: {winRate:F1}%"
                    : $"[RankMod] {username} -> Wins: {wins} | Games Played: {gamesPlayed} | Win Rate: {winRate:F1}%";

                SendPrivateMessage(recipientId, message);
            }
            else
            {
                SendPrivateMessage(recipientId, "Player data not found!");
            }
        }

        /// <summary>
        /// Handles the help command, displaying available commands based on the user's permissions.
        /// Supports paginated help messages.
        /// </summary>
        private static void HandleHelpCommand(string[] arguments, ulong senderId)
        {
            bool isAdmin = senderId == clientId; // Check if sender is an admin
            int commandsPerPage = 5;

            // Dictionary of all available player commands
            Dictionary<string, string> playerCommands = new()
            {
                { $"{commandSymbol}help", "Display all commands" },
                { $"{commandSymbol}elo", "Show Elo ranking" },
                { $"{commandSymbol}dev", "Show developer info" },
                { $"{commandSymbol}leaderboard", "Display top players" },
                { $"{commandSymbol}win", "Display wins and winrate" },
            };

                    // Dictionary of admin-only commands
                    Dictionary<string, string> adminCommands = new()
            {
                { $"{commandSymbol}ranked", "Toggle ranked mode on/off" },
                { $"{commandSymbol}lastElo", "Restore last Elo" }
            };

            // Merge player and admin commands if the sender is an admin
            Dictionary<string, string> availableCommands = new(playerCommands);
            if (isAdmin)
            {
                foreach (var cmd in adminCommands)
                    availableCommands[cmd.Key] = cmd.Value;
            }

            // Automatically split commands into pages (5 commands per page)
            List<List<KeyValuePair<string, string>>> helpPages = availableCommands
                .Select((cmd, index) => new { cmd, index })
                .GroupBy(x => x.index / commandsPerPage)
                .Select(g => g.Select(x => x.cmd).ToList())
                .ToList();

            // Validate page number
            int requestedPage = 1;
            if (arguments.Length == 2)
            {
                if (!int.TryParse(arguments[1], out requestedPage) || requestedPage < 1 || requestedPage > helpPages.Count)
                {
                    SendPrivateMessage(senderId, $"Invalid page number! Use {commandSymbol}help 1 to {commandSymbol}help {helpPages.Count}.");
                    return;
                }
            }

            // Send the header
            SendPrivateMessage(senderId, $"**[RankMod] HELP PAGE {requestedPage}/{helpPages.Count} [RankMod]**");

            // Send each command as a separate message
            foreach (var command in helpPages[requestedPage - 1])
            {
                SendPrivateMessage(senderId, $"{command.Key} - {command.Value}");
            }
        }

        /// <summary>
        /// Handles the dev command, displaying information about the developer.
        /// </summary>
        private static void HandleDevCommand(string[] arguments, ulong senderId)
        {
            string devInfo = "\n" +
                             "[RankMod] Developed by Gibson\n" +
                             "GitHub: GibsonFR\n" +
                             "Discord: gib_son";

            SendPrivateMessage(senderId, devInfo);
        }

        /// <summary>
        /// Sends an error message when a command is used incorrectly.
        /// </summary>
        private static void SendInvalidCommandMessage(ulong recipientId, string usage)
        {
            SendPrivateMessage(recipientId, $"Invalid, use: {usage}");
        }
    }
}
