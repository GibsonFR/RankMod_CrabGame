using static RankMod.MainConstants;
using static RankMod.MainUtility;

namespace RankMod
{
    public class MainConstants
    {

    }

    public class MainManager : MonoBehaviour
    {
        float elapsed = 0f;
        void FixedUpdate()
        {
            elapsed += Time.deltaTime;

            if (elapsed > 3f)
            {
                elapsed = 0f;
                ReadConfigFile();
            }
        }
    }

    public class MainPatches
    {
        [HarmonyPatch(typeof(SteamManager), nameof(SteamManager.Awake))]
        [HarmonyPostfix]
        public static void OnSteamManagerAwakePost(SteamManager __instance)
        {
            clientId = (ulong)__instance.field_Private_CSteamID_0; // Get&Set the steamId of the mod owner
        }

        [HarmonyPatch(typeof(LobbyManager), nameof(LobbyManager.AddPlayerToLobby))]
        [HarmonyPostfix]
        public static void OnLobbyManagerAddPlayerToLobbyPost(CSteamID __0)
        {
            ulong steamId = (ulong)__0;

            if (!connectedPlayers.ContainsKey(steamId)) connectedPlayers.Add(steamId, null);

            CreatePlayerData(steamId);
        }

        [HarmonyPatch(typeof(LobbyManager), nameof(LobbyManager.RemovePlayerFromLobby))]
        [HarmonyPrefix]
        public static void OnLobbyManagerRemovePlayerFromLobbyPre(CSteamID __0)
        {
            ulong steamId = (ulong)__0;

            if (connectedPlayers.ContainsKey(steamId)) connectedPlayers.Remove(steamId);
        }

        [HarmonyPatch(typeof(GameManager), nameof(GameManager.SpawnPlayer))]
        [HarmonyPostfix]
        public static void OnGameManagerSpawnPlayerPost(ulong __0)
        {          
            if (connectedPlayers.ContainsKey(__0)) connectedPlayers[__0] = GetPlayerManagerFromSteamId(__0);            
        }
    }
    public class MainUtility
    {
        public static void CreatePlayerData(ulong steamId)
        {
            // Use the shared DatabaseManager instance
            var dbManager = Database._instance;

            dbManager.AddNewPlayer(new PlayerData
            {
                ClientId = steamId,
                Username = SteamFriends.GetFriendPersonaName((CSteamID)steamId),
                Elo = 1000,
            });
        }
    }
}
