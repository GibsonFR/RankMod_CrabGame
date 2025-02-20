namespace RankMod
{
    public class DatabaseManager : MonoBehaviour
    {
        void Awake()
        {
            if (!IsHost()) return;
            Database._instance.SaveToFile();
        }

        void Update()
        {
            if (!IsHost()) return;
           
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

        public void InsertOrUpdatePlayerData(PlayerData playerData)
        {
            _database.AddOrUpdate(playerData.ClientId, playerData, (key, existingPlayer) =>
            {
                existingPlayer.Username = playerData.Username;
                existingPlayer.Elo = playerData.Elo;
                return existingPlayer;
            });
        }

        public bool SetData(ulong playerId, string propertyName, object newValue)
        {
            if (_database.TryGetValue(playerId, out var player))
            {
                switch (propertyName)
                {
                    case "Username":
                        if (newValue is string newUsername)
                        {
                            player.Username = newUsername;
                            return true;
                        }
                        break;
                    case "Elo":
                        if (newValue is float newElo)
                        {
                            player.Elo = newElo;
                            return true;
                        }
                        break;

                    default:
                        Log(logFilePath, $"Property '{propertyName}' is not recognized or has an invalid type.");
                        break;
                }
            }
            else
            {
                Log(logFilePath, $"Player with ClientId {playerId} not found.");
            }

            return false;
        }

        public bool AddNewPlayer(PlayerData playerData)
        {
            // Check if the player already exists
            if (_database.ContainsKey(playerData.ClientId))
            {
                Log(logFilePath, $"Player with ClientId {playerData.ClientId} already exists.");
                return false; 
            }

            // Add the new player
            if (_database.TryAdd(playerData.ClientId, playerData))
            {
                Log(logFilePath, $"Player {playerData.Username} (ClientId: {playerData.ClientId}) added successfully.");
                return true; 
            }

            Log(logFilePath, $"Failed to add player {playerData.Username} (ClientId: {playerData.ClientId}).");
            return false; 
        }

        public IEnumerable<PlayerData> GetAllPlayers()
        {
            return _database.Values;
        }

        public PlayerData GetPlayerData(ulong playerId)
        {
            if (_database.TryGetValue(playerId, out var playerData))
            {
                return playerData;
            }

            return null;
        }


        public void SaveToFile()
        {
            try
            {
                Log(logFilePath, $"SaveToFile called. Saving {_database.Count} players to {_filePath}");

                using var writer = new StreamWriter(_filePath);
                foreach (var player in _database.Values)
                {
                    Log(logFilePath, $"Saving player: {player.ClientId} | {player.Username} | {player.Elo}");
                    writer.WriteLine($"{player.ClientId}|{player.Username}|{player.Elo}");
                }

                Log(logFilePath, "SaveToFile completed successfully.");
            }
            catch (Exception ex)
            {
                Log(logFilePath, $"Error during save: {ex.Message}");
            }
        }

        private ConcurrentDictionary<ulong, PlayerData> LoadFromFile()
        {
            try
            {
                if (!File.Exists(_filePath))
                {
                    return new ConcurrentDictionary<ulong, PlayerData>();
                }

                var lines = File.ReadAllLines(_filePath);
                var data = lines
                    .Select(line =>
                    {
                        var parts = line.Split('|');
                        if (parts.Length != 3) return null;

                        return new PlayerData
                        {
                            ClientId = ulong.TryParse(parts[0], out var clientId) ? clientId : 0,
                            Username = parts[1],
                            Elo = float.Parse(parts[2])
                        };
                    })
                    .Where(player => player != null)
                    .ToDictionary(player => player.ClientId, player => player);

                return new ConcurrentDictionary<ulong, PlayerData>(data);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading database file: {ex.Message}");
                return new ConcurrentDictionary<ulong, PlayerData>();
            }
        }
    }
    public class PlayerData
    {
        public ulong ClientId { get; set; }
        public string Username { get; set; } 
        public float Elo { get; set; }
    }
}
