using static RankMod.MainConstants;
using static RankMod.MainUtility;

namespace RankMod
{
    public class MainConstants
    {

    }

    public class MainManager : MonoBehaviour
    {
        /// <summary>
        /// Reads the configuration file to ensure up-to-date settings each Round.
        /// </summary>
        void Awake()
        {
            ReadConfigFile();
        }
    }

    public class MainPatches
    {
        /// <summary>
        /// Retrieves and sets the Steam ID of the mod owner when SteamManager initializes.
        /// </summary>
        [HarmonyPatch(typeof(SteamManager), nameof(SteamManager.Awake))]
        [HarmonyPostfix]
        public static void OnSteamManagerAwakePost(SteamManager __instance)
        {
            if (clientId < 1)
            {
                clientId = (ulong)__instance.field_Private_CSteamID_0;
            }
        }

        /// <summary>
        /// Adds a player to the connected players list and initializes their data when they join the lobby.
        /// </summary>
        [HarmonyPatch(typeof(LobbyManager), nameof(LobbyManager.AddPlayerToLobby))]
        [HarmonyPostfix]
        public static void OnLobbyManagerAddPlayerToLobbyPost(CSteamID __0)
        {
            ulong steamId = (ulong)__0;

            if (!connectedPlayers.ContainsKey(steamId))
            {
                connectedPlayers.Add(steamId, null);
            }

            CreatePlayerData(steamId);
        }

        /// <summary>
        /// Removes a player from the connected players list when they leave the lobby.
        /// </summary>
        [HarmonyPatch(typeof(LobbyManager), nameof(LobbyManager.RemovePlayerFromLobby))]
        [HarmonyPrefix]
        public static void OnLobbyManagerRemovePlayerFromLobbyPre(CSteamID __0)
        {
            ulong steamId = (ulong)__0;

            if (connectedPlayers.ContainsKey(steamId))
            {
                connectedPlayers.Remove(steamId);
            }
        }

        /// <summary>
        /// Updates the player's reference in the connected players list when they spawn in the game.
        /// </summary>
        [HarmonyPatch(typeof(GameManager), nameof(GameManager.SpawnPlayer))]
        [HarmonyPostfix]
        public static void OnGameManagerSpawnPlayerPost(ulong __0)
        {
            if (connectedPlayers.ContainsKey(__0))
            {
                connectedPlayers[__0] = GetPlayerManagerFromSteamId(__0);
            }
        }
    }
    public class MainUtility
    {
        /// <summary>
        /// Creates a new player entry in the database if it does not already exist.
        /// </summary>
        public static void CreatePlayerData(ulong steamId)
        {
            // Use the shared DatabaseManager instance
            var dbManager = Database._instance;

            // Create a new player with dynamic property initialization
            var newPlayer = new PlayerData { ClientId = steamId };

            // Set initial properties dynamically
            newPlayer.Properties["Username"] = SteamFriends.GetFriendPersonaName((CSteamID)steamId).Replace("|", "");

            dbManager.AddNewPlayer(newPlayer);
        }

    }
}
