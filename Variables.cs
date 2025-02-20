namespace RankMod
{
    public static class Variables
    {
        // folder
        public static string assemblyFolderPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        public static string defaultFolderPath = assemblyFolderPath + "\\";
        public static string mainFolderPath = defaultFolderPath + @"RankMod\";
        public static string playersDataFolderPath = mainFolderPath + @"PlayersData\";

        // file
        public static string logFilePath = mainFolderPath + "log.txt";
        public static string playersDataFilePath = playersDataFolderPath + "database.txt";
        public static string configFilePath = mainFolderPath + "config.txt";

        // Dictionnary
        public static Dictionary<ulong, PlayerManager> connectedPlayers = [];
        public static List<ulong> playersInRanked = [];

        // ulong
        public static ulong clientId;

        // int
        public static int playersThisGame;

        // float
        public static float kFactor, totalGameExpectative, averageGameElo, eloScalingFactor, initialElo;

        // bool
        public static bool ranked, gameHasStarted;
    }
}
