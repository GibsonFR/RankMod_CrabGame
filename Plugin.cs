global using BepInEx;
global using BepInEx.IL2CPP;
global using HarmonyLib;
global using SteamworksNative;
global using System;
global using System.Collections.Concurrent;
global using System.Collections.Generic;
global using System.Linq;
global using System.IO;
global using System.Reflection;
global using UnityEngine;
global using UnhollowerRuntimeLib;
global using System.Globalization;
global using System.Text;

global using static RankMod.Variables;
global using static RankMod.Utility;

namespace RankMod
{
    [BepInPlugin("D8E1467C-3801-44A2-B081-9D40D920778F", "RankMod", "1.1.2")]
    public class Plugin : BasePlugin
    {
        /// <summary>
        /// Initializes and loads the RankMod plugin.
        /// Sets up necessary folders, files, database migrations, and patches for the mod.
        /// </summary>
        public override void Load()
        {
            ClassInjector.RegisterTypeInIl2Cpp<MainManager>();
            ClassInjector.RegisterTypeInIl2Cpp<DatabaseManager>();
            ClassInjector.RegisterTypeInIl2Cpp<RankSystemManager>();

            Harmony.CreateAndPatchAll(typeof(Plugin));
            Harmony harmony = new("gibson.rank");
            harmony.PatchAll(typeof(MainPatches));
            harmony.PatchAll(typeof(RankSystemPatches));
            harmony.PatchAll(typeof(CommandPatches));

            CreateFolder(mainFolderPath);
            CreateFolder(playersDataFolderPath);
            CreateFile(playersDataFilePath);

            CreateFile(logFilePath);
            ResetFile(logFilePath);

            CreateFile(configFilePath);
            SetConfigFile(configFilePath);

            Database.ConvertOldDataFile(playersDataFilePath);
            Database.UpdateProperties(playersDataFilePath);

            Log.LogInfo("Mod created by Gibson, discord : gib_son, github : GibsonFR");
        }


        /// <summary>
        /// Patches the GameUI Awake method to inject RankMod components into the game's UI hierarchy.
        /// Ensures that essential managers are properly instantiated when the UI loads.
        /// </summary>
        [HarmonyPatch(typeof(GameUI), nameof(GameUI.Awake))]
        [HarmonyPostfix]
        public static void UIAwakePatch(GameUI __instance)
        {
            GameObject pluginObj = new();
            pluginObj.transform.SetParent(__instance.transform);

            pluginObj.AddComponent<MainManager>();
            pluginObj.AddComponent<DatabaseManager>();
            pluginObj.AddComponent<RankSystemManager>();
        }

        /// <summary>
        /// Anticheat Bypass.
        /// <summary>
        [HarmonyPatch(typeof(EffectManager), "Method_Private_Void_GameObject_Boolean_Vector3_Quaternion_0")]
        [HarmonyPatch(typeof(LobbyManager), "Method_Private_Void_0")]
        [HarmonyPatch(typeof(MonoBehaviourPublicVesnUnique), "Method_Private_Void_0")]
        [HarmonyPatch(typeof(LobbySettings), "Method_Public_Void_PDM_2")]
        [HarmonyPatch(typeof(MonoBehaviourPublicTeplUnique), "Method_Private_Void_PDM_32")]
        [HarmonyPrefix]
        public static bool Prefix(MethodBase __originalMethod)
        {
            return false;
        }
    }
}