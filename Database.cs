namespace RankMod
{
    public class DatabaseManager : MonoBehaviour
    {
        void Awake()
        {
            if (!IsHost()) return;
            Database._instance.SaveToFile();
        }
    }

    public class Database
    {
        public static Database _instance = new Database(playersDataFilePath);
        private readonly string _filePath;
        private readonly ConcurrentDictionary<ulong, PlayerData> _database;

        private Database(string filePath)
        {
            _filePath = filePath;
            _database = LoadFromFile();
        }

        /// <summary>
        /// Converts lines of the old format (3 parts: SteamID|Username|Elo)
        /// into the new format (SteamID|Username:XXX|Elo:YYY).
        /// If a line does NOT have exactly 3 parts, it's left untouched.
        /// This does NOT add LastElo or other properties.
        /// </summary>
        public static void ConvertOldDataFile(string _filePath)
        {
            if (!File.Exists(_filePath)) return;

            var lines = File.ReadAllLines(_filePath);
            var newLines = new List<string>(lines.Length);

            foreach (var line in lines)
            {
                var parts = line.Split('|');

                // Detect old format: exactly 3 parts => SteamID | Username | Elo
                if (parts.Length == 3 && ulong.TryParse(parts[0], out ulong steamId))
                {
                    // The second element is the username
                    string username = parts[1];
                    // The third element is the elo
                    if (!float.TryParse(parts[2], out float elo))
                        elo = 1000f;

                    // Rewrite to new format
                    string convertedLine = $"{steamId}|Username:{username}|Elo:{elo}";
                    newLines.Add(convertedLine);
                }
                else
                {
                    // Keep lines that are already new format or otherwise not 3 parts
                    newLines.Add(line);
                }
            }

            File.WriteAllLines(_filePath, newLines);
        }


        /// <summary>
        /// Updates the database file directly by ensuring that all default properties exist
        /// and removing outdated properties. This is called BEFORE LoadFromFile().
        /// </summary>
        public static void UpdateProperties(string _filePath)
        {
            if (!File.Exists(_filePath)) return;

            var lines = File.ReadAllLines(_filePath);
            var updatedLines = new List<string>();
            bool updated = false;

            foreach (var line in lines)
            {
                var parts = line.Split('|');
                if (parts.Length < 2) continue; 

                ulong clientId;
                if (!ulong.TryParse(parts[0], out clientId)) continue; 

                // Convert existing properties into a dictionary
                var playerProperties = new Dictionary<string, string>();

                foreach (var part in parts.Skip(1))
                {
                    var keyValue = part.Split(':', 2);
                    if (keyValue.Length == 2)
                    {
                        playerProperties[keyValue[0]] = keyValue[1];
                    }
                }

                // Ensure all default properties exist
                foreach (var kvp in PlayerData.DefaultProperties)
                {
                    if (!playerProperties.ContainsKey(kvp.Key))
                    {
                        playerProperties[kvp.Key] = kvp.Value.ToString(); // Convert default value to string
                        updated = true;
                    }
                }

                // Remove properties that are no longer in the default set
                var propertiesToRemove = playerProperties.Keys.Except(PlayerData.DefaultProperties.Keys).ToList();
                foreach (var prop in propertiesToRemove)
                {
                    playerProperties.Remove(prop);
                    updated = true;
                }

                // Construct the updated line
                string updatedLine = $"{clientId}|" + string.Join("|", playerProperties.Select(kvp => $"{kvp.Key}:{kvp.Value}"));
                updatedLines.Add(updatedLine);
            }

            if (updated)
            {
                File.WriteAllLines(_filePath, updatedLines);
            }
        }



        /// <summary>
        /// Inserts or updates a player's data in the database.
        /// </summary>
        public void InsertOrUpdatePlayerData(PlayerData playerData)
        {
            _database.AddOrUpdate(playerData.ClientId, playerData, (key, existingPlayer) =>
            {
                existingPlayer.Properties = playerData.Properties;
                return existingPlayer;
            });
        }

        /// <summary>
        /// Updates a specific property of a player.
        /// </summary>
        public bool SetData(ulong playerId, string propertyName, object newValue)
        {
            if (_database.TryGetValue(playerId, out var player))
            {
                player.Properties[propertyName] = newValue;
                return true;
            }

            Log(logFilePath, $"Player with ClientId {playerId} not found.");
            return false;
        }

        /// <summary>
        /// Adds a new player to the database if they do not already exist.
        /// </summary>
        public bool AddNewPlayer(PlayerData playerData)
        {
            if (_database.ContainsKey(playerData.ClientId))
            {
                Log(logFilePath, $"Player with ClientId {playerData.ClientId} already exists.");
                return false;
            }

            if (_database.TryAdd(playerData.ClientId, playerData))
            {
                Log(logFilePath, $"Player {playerData.Properties["Username"]} (ClientId: {playerData.ClientId}) added successfully.");
                return true;
            }

            Log(logFilePath, $"Failed to add player {playerData.Properties["Username"]} (ClientId: {playerData.ClientId}).");
            return false;
        }

        /// <summary>
        /// Retrieves all players from the database.
        /// </summary>
        public IEnumerable<PlayerData> GetAllPlayers() => _database.Values;

        /// <summary>
        /// Retrieves a specific player's data by ClientId.
        /// </summary>
        public PlayerData GetPlayerData(ulong playerId) => _database.TryGetValue(playerId, out var playerData) ? playerData : null;

        /// <summary>
        /// Saves the entire database to a file.
        /// </summary>
        public void SaveToFile()
        {
            try
            {
                Log(logFilePath, $"Saving {_database.Count} players to {_filePath}");
                using var writer = new StreamWriter(_filePath);
                foreach (var player in _database.Values)
                {
                    string props = string.Join("|", player.Properties.Select(kvp => $"{kvp.Key}:{kvp.Value}"));
                    string line = $"{player.ClientId}|" + props;
                    writer.WriteLine(line);
                }
                Log(logFilePath, "Database saved successfully.");
            }
            catch (Exception ex)
            {
                Log(logFilePath, $"Error saving database: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads player data from a file and returns a concurrent dictionary.
        /// </summary>
        private ConcurrentDictionary<ulong, PlayerData> LoadFromFile()
        {
            try
            {
                if (!File.Exists(_filePath))
                {
                    return new ConcurrentDictionary<ulong, PlayerData>();
                }

                var lines = File.ReadAllLines(_filePath);
                var data = new ConcurrentDictionary<ulong, PlayerData>();

                foreach (var line in lines)
                {
                    var parts = line.Split('|');
                    ulong clientId = ulong.TryParse(parts[0], out var id) ? id : 0;
                    if (clientId == 0) continue;

                    var playerData = new PlayerData { ClientId = clientId };

                    foreach (var part in parts.Skip(1))
                    {
                        var keyValue = part.Split(':');
                        if (keyValue.Length == 2)
                        {
                            if (float.TryParse(keyValue[1], out var numericValue))
                                playerData.Properties[keyValue[0]] = numericValue;
                            else
                                playerData.Properties[keyValue[0]] = keyValue[1];
                        }
                    }

                    data.TryAdd(clientId, playerData);
                }

                return data;
            }
            catch (Exception ex)
            {
                Log(logFilePath, $"Error loading database file: {ex.Message}");
                return new ConcurrentDictionary<ulong, PlayerData>();
            }
        }
    }

    public class PlayerData
    {
        public ulong ClientId { get; set; }

        // Dictionary containing all player properties
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();

        // Centralized list of all properties with default values
        public static readonly Dictionary<string, object> DefaultProperties = new Dictionary<string, object>
        {
            { "Username", "Unknown" },
            { "Elo", 1000f },
            { "LastElo", 1000f },
        };

        public PlayerData()
        {
            // Initialize Properties with default values dynamically
            foreach (var prop in DefaultProperties)
            {
                if (!Properties.ContainsKey(prop.Key))
                {
                    Properties[prop.Key] = prop.Value;
                }
            }
        }
    }
}
