namespace RankMod
{
    public static class Utility
    {
        /// <summary>
        /// Creates a directory if it does not already exist.
        /// </summary>
        public static void CreateFolder(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }
            catch (Exception ex)
            {
                Log(logFilePath, "Error creating folder: " + ex.Message);
            }
        }

        /// <summary>
        /// Creates a file if it does not already exist.
        /// </summary>
        public static void CreateFile(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    using StreamWriter sw = File.CreateText(path);
                    sw.WriteLine("");
                }
            }
            catch (Exception ex)
            {
                Log(logFilePath, "Error creating file: " + ex.Message);
            }
        }

        /// <summary>
        /// Resets a file by clearing its contents if it exists.
        /// </summary>
        public static void ResetFile(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    using StreamWriter sw = new(path, false);
                }
            }
            catch (Exception ex)
            {
                Log(logFilePath, "Error resetting file: " + ex.Message);
            }
        }

        /// <summary>
        /// Writes a log entry to the specified log file.
        /// </summary>
        public static void Log(string path, string line)
        {
            try
            {
                using StreamWriter writer = new(path, true);
                writer.WriteLine(line.Trim());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Log Error] Failed to write log: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks if the client is the host of the game.
        /// </summary>
        public static bool IsHost()
        {
            return SteamManager.Instance.IsLobbyOwner();
        }

        /// <summary>
        /// Retrieves the PlayerManager instance for a given SteamID.
        /// Searches both active players and spectators.
        /// </summary>
        public static PlayerManager GetPlayerManagerFromSteamId(ulong steamId)
        {
            foreach (var player in GameManager.Instance.activePlayers)
            {
                try
                {
                    if (player.value.steamProfile.m_SteamID == steamId)
                        return player.value;
                }
                catch (Exception ex)
                {
                    Log(logFilePath, $"Error retrieving PlayerManager from activePlayers for SteamID {steamId}: {ex.Message}");
                }
            }

            foreach (var player in GameManager.Instance.spectators)
            {
                try
                {
                    if (player.value.steamProfile.m_SteamID == steamId)
                        return player.value;
                }
                catch (Exception ex)
                {
                    Log(logFilePath, $"Error retrieving PlayerManager from spectators for SteamID {steamId}: {ex.Message}");
                }
            }

            return null;
        }

        /// <summary>
        /// Retrieves the current game state as a string.
        /// </summary>
        public static string GetGameState()
        {
            try
            {
                return GameManager.Instance.gameMode.modeState.ToString();
            }
            catch (Exception ex)
            {
                Log(logFilePath, $"Error retrieving game state: {ex.Message}");
                return ""; // Default fallback value
            }
        }

        /// <summary>
        /// Retrieves a list of alive players (not dead and not at position zero).
        /// </summary>
        public static List<ulong> GetPlayerAliveList()
        {
            return connectedPlayers
                .Where(player => player.Value != null
                              && !player.Value.dead
                              && player.Value.transform.position != Vector3.zero)
                .Select(player => player.Value.steamProfile.m_SteamID)
                .ToList();
        }

        /// <summary>
        /// Sends a private chat message to a specific player.
        /// Logs an error if message sending fails.
        /// Special thanks to github.com/lammas321 for contributions.
        /// </summary>
        public static void SendPrivateMessage(ulong clientId, string message)
        {
            try
            {
                string privateMessage = $"{message}";
                List<byte> bytes = [];
                bytes.AddRange(BitConverter.GetBytes((int)ServerSendType.sendMessage));
                bytes.AddRange(BitConverter.GetBytes((ulong)1));

                string username = SteamFriends.GetFriendPersonaName(new CSteamID(1));
                bytes.AddRange(BitConverter.GetBytes(username.Length));
                bytes.AddRange(Encoding.ASCII.GetBytes(username));

                bytes.AddRange(BitConverter.GetBytes(privateMessage.Length));
                bytes.AddRange(Encoding.ASCII.GetBytes(privateMessage));

                bytes.InsertRange(0, BitConverter.GetBytes(bytes.Count));

                Packet packet = new()
                {
                    field_Private_List_1_Byte_0 = new()
                };
                foreach (byte b in bytes)
                    packet.field_Private_List_1_Byte_0.Add(b);

                byte[] clientIdBytes = BitConverter.GetBytes(clientId);
                for (int i = 0; i < clientIdBytes.Length; i++)
                    packet.field_Private_List_1_Byte_0[i + 8] = clientIdBytes[i];

                SteamPacketManager.SendPacket(new CSteamID(clientId), packet, 8, SteamPacketDestination.ToClient);
            }
            catch (Exception ex)
            {
                Log(logFilePath, $"Error sending private message to {clientId}: {ex.Message}");
            }
        }

        /// <summary>
        /// Forces a message to appear in the client chat only.
        /// </summary>
        public static void ForceMessage(string message)
        {
            try
            {
                ChatBox.Instance.ForceMessage(message);
            }
            catch (Exception ex)
            {
                Log(logFilePath, $"Error forcing message: {ex.Message}");
            }
        }

        /// <summary>
        /// Sends a server-wide chat message visible to all players.
        /// </summary>
        public static void SendServerMessage(string message)
        {
            try
            {
                ServerSend.SendChatMessage(1, $"{message}");
            }
            catch (Exception ex)
            {
                Log(logFilePath, $"Error sending server message: {ex.Message}");
            }
        }

        /// <summary>
        /// Creates or updates the configuration file with default values if missing.
        /// </summary>
        public static void SetConfigFile(string configFilePath)
        {
            try
            {
                Dictionary<string, string> configDefaults = new()
                {
                    {"version", "v1.1.3"},
                    {"rankedOnStart", "true"},
                    {"initialElo", "1000"},
                    {"kFactor", "32"},
                    {"eloScalingFactor", "100"},
                    {"commandSymbol", "." }
                };

                Dictionary<string, string> currentConfig = [];

                if (File.Exists(configFilePath))
                {
                    string[] lines = File.ReadAllLines(configFilePath);

                    foreach (string line in lines)
                    {
                        string[] keyValue = line.Split('=');
                        if (keyValue.Length == 2)
                        {
                            currentConfig[keyValue[0]] = keyValue[1];
                        }
                    }
                }

                foreach (KeyValuePair<string, string> pair in configDefaults)
                {
                    if (!currentConfig.ContainsKey(pair.Key))
                    {
                        currentConfig[pair.Key] = pair.Value;
                    }
                }

                using StreamWriter sw = File.CreateText(configFilePath);
                foreach (KeyValuePair<string, string> pair in currentConfig)
                {
                    sw.WriteLine(pair.Key + "=" + pair.Value);
                }
            }
            catch (Exception ex)
            {
                Log(logFilePath, $"Error setting config file: {ex.Message}");
            }
        }

        /// <summary>
        /// Reads the configuration file and applies settings to the application.
        /// </summary>
        public static void ReadConfigFile()
        {
            try
            {
                if (!File.Exists(configFilePath))
                {
                    Log(logFilePath, "Config file not found.");
                    return;
                }

                string[] lines = File.ReadAllLines(configFilePath);
                Dictionary<string, string> config = [];
                _ = new CultureInfo("fr-FR");
                bool parseSuccess;

                foreach (string line in lines)
                {
                    string[] parts = line.Split('=');
                    if (parts.Length == 2)
                    {
                        string key = parts[0].Trim();
                        string value = parts[1].Trim();
                        config[key] = value;
                    }
                }

                if (configStart)
                {
                    parseSuccess = bool.TryParse(config.GetValueOrDefault("rankedOnStart", "true"), out bool resultBool);
                    ranked = parseSuccess ? resultBool : true;

                    configStart = false;
                }

                parseSuccess = float.TryParse(config.GetValueOrDefault("initialElo", "1000"), out float resultFloat);
                initialElo = parseSuccess ? resultFloat : 1000;

                parseSuccess = float.TryParse(config.GetValueOrDefault("kFactor", "32"), out resultFloat);
                kFactor = parseSuccess ? resultFloat : 32;

                parseSuccess = float.TryParse(config.GetValueOrDefault("eloScalingFactor", "100"), out resultFloat);
                eloScalingFactor = parseSuccess ? resultFloat : 100;

                commandSymbol = config.GetValueOrDefault("commandSymbol", ".");
            }
            catch (Exception ex)
            {
                Log(logFilePath, $"Error reading config file: {ex.Message}");
            }
        }
    }
}
