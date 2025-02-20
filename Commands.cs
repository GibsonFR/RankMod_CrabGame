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

                if (__0 == clientId) ExecuteCommand(parts[0], parts, __0, isAdmin: true);
                else ExecuteCommand(parts[0], parts, __0, isAdmin: false);
                return false;
            }
            return true;
        }

        private static void ExecuteCommand(string command, string[] arguments, ulong senderId, bool isAdmin)
        {
            var adminCommands = new Dictionary<string, Action<string[], ulong>>
            {
                { "!ranked", HandleRankedCommand },
                { "/ranked", HandleRankedCommand }
            };

            // Liste des commandes accessibles aux joueurs
            var playerCommands = new Dictionary<string, Action<string[], ulong>>
            {
                { "!elo", HandleEloCommand },
                { "/elo", HandleEloCommand },
                { "!help", HandleHelpCommand },
                { "/help", HandleHelpCommand }
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
                SendPrivateMessage(senderId, "Unknown command! Try !help for a list of commands.");
            }
        }


        static bool IsCommand(string msg) => msg.StartsWith("!") || msg.StartsWith("/");

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
                .FirstOrDefault(player => player.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

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
                SendInvalidCommandMessage(senderId, "!elo | !elo #playerNumber");
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
                string message = (recipientId == playerId)
                    ? $"{playerData.Username}, your Elo is: {playerData.Elo}"
                    : $"{playerData.Username} Elo: {playerData.Elo}";

                SendPrivateMessage(recipientId, message);
            }
            else
            {
                SendPrivateMessage(recipientId, "Player data not found!");
            }
        }

        private static void HandleHelpCommand(string[] arguments, ulong senderId)
        {
            string lineBreaks = string.Concat(Enumerable.Repeat("|\n", 6)); // Générer 9 sauts de ligne

            // Liste des commandes et descriptions
            List<string> helpPages = new()
            {
                lineBreaks + "**Help Page 1/2**\n" +
                "!help\n"  +
                "!elo\n"   +
                "-- --\n"  +
                "-- --\n"  +
                "-- --\n"  +
                "-- --\n"  +
                "-- --\n", 

                lineBreaks + "**Help Page 2/2**\n" +
                "-- --\n" +
                "-- --\n" +
                "-- --\n" +
                "-- --\n" +
                "-- --\n" +
                "-- --\n" +
                "-- --"
            };

            int requestedPage = 1;
            if (arguments.Length == 2 && int.TryParse(arguments[1], out int pageNumber))
            {
                requestedPage = Math.Clamp(pageNumber, 1, helpPages.Count);
            }

            SendPrivateMessage(senderId, helpPages[requestedPage - 1]);
        }



        private static void SendInvalidCommandMessage(ulong recipientId, string usage)
        {
            SendPrivateMessage(recipientId, $"Invalid, use: {usage}");
        }
    }
}
