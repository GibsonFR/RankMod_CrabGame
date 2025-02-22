namespace RankMod
{
    public static class Utility
    {

        public static void CreateFolder(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }
            catch { }
        }

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
            catch { }
        }

        public static void ResetFile(string path)
        {
            try
            {
                // Vérifier si le fichier existe
                if (File.Exists(path))
                {
                    using StreamWriter sw = new(path, false);
                }
            }
            catch { }
        }

        public static void Log(string path, string line)
        {
            using StreamWriter writer = new(path, true);
            writer.WriteLine(line.Trim()); 
        }

        public static bool IsHost()
        {
            return SteamManager.Instance.IsLobbyOwner();
        }

        public static PlayerManager GetPlayerManagerFromSteamId(ulong steamId)
        {
            foreach (var player in GameManager.Instance.activePlayers)
            {
                try
                {
                    if (player.value.steamProfile.m_SteamID == steamId) return player.value;
                }
                catch { }
            }
            foreach (var player in GameManager.Instance.spectators)
            {
                try
                {
                    if (player.value.steamProfile.m_SteamID  == steamId)
                        return player.value;
                }
                catch { }
            }
            return null;
        }

        public static int GetCurrentGameTimer()
        {
            return UnityEngine.Object.FindObjectOfType<TimerUI>().field_Private_TimeSpan_0.Seconds;
        }

        public static string GetGameState()
        {
            return GameManager.Instance.gameMode.modeState.ToString();
        }

        //Thanks to github.com/lammas321
        public static void SendPrivateMessage(ulong clientId, string message)
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

        public static void ForceMessage(string message)
        {
            ChatBox.Instance.ForceMessage(message);
        }

        public static void SendServerMessage(string message)
        {
            ServerSend.SendChatMessage(1, $"{message}");
        }
        public static void SetConfigFile(string configFilePath)
        {
            Dictionary<string, string> configDefaults = new()
            {
                {"version", "v1.1.1"},
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
        public static void ReadConfigFile()
        {
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
                parseSuccess = bool.TryParse(config["rankedOnStart"], out bool resultBool);
                ranked = parseSuccess ? resultBool : true;

                configStart = false;
            }

            parseSuccess = float.TryParse(config["initialElo"], out float resultFloat);
            initialElo = parseSuccess ? resultFloat : 1000;

            parseSuccess = float.TryParse(config["kFactor"], out resultFloat);
            kFactor = parseSuccess ? resultFloat : 32;

            parseSuccess = float.TryParse(config["eloScalingFactor"], out resultFloat);
            eloScalingFactor = parseSuccess ? resultFloat : 100;

            commandSymbol = config["commandSymbol"];
        }
    }
}
