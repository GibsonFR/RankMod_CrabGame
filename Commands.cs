namespace RankMod
{
    public class CommandPatchs
    {
        [HarmonyPatch(typeof(ServerSend), nameof(ServerSend.SendChatMessage))]
        [HarmonyPrefix]
        public static bool ServerSendSendChatMessagePre(ulong __0, string __1)
        {
            if (!IsHost()) return true;

            if (IsCommand(__1))
            {
                string[] parts = __1.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0) return false;

                if (__0 == clientId) ExecuteCommand(parts[0], parts, __0, true);
                else ExecuteCommand(parts[0], parts, __0, false);
                return false;
            }
            return true;
        }

        private static void ExecuteCommand(string command, string[] arguments, ulong senderId, bool isAdmin)
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


        static bool IsCommand(string msg) => msg.StartsWith(commandSymbol);

        public static void HandleRankedCommand(string[] arguments, ulong senderId)
        {
            ranked = !ranked;
            string statusMessage = ranked ? "[RankMod] Games are now ranked" : "[RankMod] Games aren't ranked anymore";
            SendPrivateMessage(senderId, statusMessage);
        }


        public static ulong? CommandPlayerFinder(string identifier)
        {
            if (identifier.StartsWith("#") && int.TryParse(identifier.AsSpan(1), out int playerNumber))
            {
                return GetPlayerSteamId(playerNumber);
            }

            return GetPlayerSteamId(identifier);
        }

        public static ulong? GetPlayerSteamId(int playerNumber)
        {
            return connectedPlayers.Values
                .FirstOrDefault(player => player != null && player.playerNumber == playerNumber)
                ?.steamProfile.m_SteamID;
        }

        public static ulong? GetPlayerSteamId(string username)
        {
            var playerEntry = Database._instance.GetAllPlayers()
                .FirstOrDefault(player =>
                    player.Properties.TryGetValue("Username", out var storedUsername) &&
                    storedUsername is string storedName &&
                    storedName.Equals(username, StringComparison.OrdinalIgnoreCase));

            return playerEntry?.ClientId;
        }


        public static PlayerManager GetPlayer(ulong steamId)
        {
            if (connectedPlayers.TryGetValue(steamId, out PlayerManager player))
            {
                return player;
            }

            return null;
        }

        private static void HandleEloCommand(string[] arguments, ulong senderId)
        {
            if (arguments.Length > 2)
            {
                SendInvalidCommandMessage(senderId, $"{commandSymbol}elo | {commandSymbol}elo #playerNumber");
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

        private static void SendEloMessage(ulong recipientId, ulong playerId)
        {
            PlayerData playerData = Database._instance?.GetPlayerData(playerId);

            if (playerData != null)
            {
                // Retrieve Username and Elo dynamically from the Properties dictionary
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
                SendInvalidCommandMessage(senderId, "!leaderboard | !leaderboard player | !leaderboard top");
            }
        }

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
                SendPrivateMessage(recipientId, $"[{Database._instance?.GetPlayerData(recipientId).Properties["Rank"]}] {Database._instance?.GetPlayerData(recipientId).Properties["Username"]} leaderboard rank: {playerEntry.Rank}/{Database._instance?.GetAllPlayers().Count()}");
            }
            else
            {
                SendPrivateMessage(recipientId, "Player not found in the leaderboard.");
            }
        }

        private static void SendTop5Leaderboard(ulong recipientId)
        {
            var leaderboard = Database._instance?.GetAllPlayers()
                .Where(p => p.Properties.TryGetValue("Elo", out var eloValue) && eloValue is float)
                .OrderByDescending(p => (float)p.Properties["Elo"])
                .Take(5)
                .Select((player, index) => new { Top = index + 1, Rank = player.Properties["Rank"], Username = player.Properties["Username"], Elo = player.Properties["Elo"] })
                .ToList();

            if (leaderboard != null && leaderboard.Count > 0)
            {
                SendPrivateMessage(recipientId, "[RankMod] Top 5 Leaderboard:");
                foreach (var entry in leaderboard)
                {
                    SendPrivateMessage(recipientId, $"top{entry.Top} [{entry.Rank}] {entry.Username}: {entry.Elo:F1} Elo");
                }
            }
            else
            {
                SendPrivateMessage(recipientId, "No leaderboard data available.");
            }
        }

        private static void HandleHelpCommand(string[] arguments, ulong senderId)
        {
            bool isAdmin = senderId == clientId; // Check if sender is an admin
            int commandsPerPage = 5;

            // Dictionary of all available commands
            Dictionary<string, string> playerCommands = new()
            {
                { $"{commandSymbol}help", "Display all commands" },
                { $"{commandSymbol}elo", "Show elo ranking" },
                { $"{commandSymbol}dev", "Show developer info" },
                { $"{commandSymbol}leaderboard", "Display top players" },
            };

            Dictionary<string, string> adminCommands = new()
            {
                { $"{commandSymbol}ranked", "Toggle ranked mode on/off" },
                { $"{commandSymbol}lastElo", "Restore last elo" }
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



        private static void HandleDevCommand(string[] arguments, ulong senderId)
        {
            string devInfo = "\n" +
                             "[RankMod] Developed by Gibson\n" +
                             "GitHub: GibsonFR\n" +
                             "Discord: gib_son";

            SendPrivateMessage(senderId, devInfo);
        }

        private static void SendInvalidCommandMessage(ulong recipientId, string usage)
        {
            SendPrivateMessage(recipientId, $"Invalid, use: {usage}");
        }
    }
}
