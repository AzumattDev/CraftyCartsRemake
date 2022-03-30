using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using PieceManager;
using UnityEngine;
using ServerSync;

namespace CraftyCartsRemake
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class CCR : BaseUnityPlugin
    {
        /*
         * Special thank you to Rolopogo for the cart models and original idea. I have changed the config file to match my naming convention since almost all of the code has changed to better standards.
         * It now uses the PieceManager written by me to load the carts into the world. I rebuilt the assets inside unity to have scripts there, and not having everything done inside the code.
         * This makes it a bit less buggy on the material and works more fluidly. Rolopogo is credited in the AssemblyInfo.cs file, and here. Thank you again Rolo.
         */
        public const string ModVersion = "3.0.4";
        public const string ModGUID = "azumatt.CraftyCarts";
        public const string Author = "Azumatt";
        public const string ModName = "CraftyCarts";
        private readonly Harmony _harmony = new(ModGUID);

        internal static BuildPiece forgeCart;
        internal static BuildPiece stoneCart;
        internal static BuildPiece workbenchCart;


        private static string ConfigFileName = ModGUID + ".cfg";
        private static string ConfigFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;

        public static readonly ManualLogSource CCRLogger =
            BepInEx.Logging.Logger.CreateLogSource(ModName);

        private static readonly ConfigSync ConfigSync = new(ModGUID)
            { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion };

        public void Awake()
        {
            // _serverConfigLocked = config("General", "Force Server Config", true, "Force Server Config");
            // _ = ConfigSync.AddLockingConfigEntry(_serverConfigLocked);
            ConfigSync.IsLocked = true;

            forgeCart = new BuildPiece("craftycarts", "forge_cart");
            forgeCart.Description.English("Mobile Forge");
            forgeCart.RequiredItems.Add("Stone", 4, true);
            forgeCart.RequiredItems.Add("Coal", 4, true);
            forgeCart.RequiredItems.Add("Wood", 10, true);
            forgeCart.RequiredItems.Add("Copper", 6, true);

            stoneCart = new BuildPiece("craftycarts", "stone_cart");
            stoneCart.Description.English("Mobile Stone Cutter");
            stoneCart.RequiredItems.Add("Wood", 10, true);
            stoneCart.RequiredItems.Add("Iron", 2, true);
            stoneCart.RequiredItems.Add("Stone", 4, true);

            workbenchCart = new BuildPiece("craftycarts", "workbench_cart");
            workbenchCart.Description.English("Mobile Workbench");
            workbenchCart.RequiredItems.Add("Wood", 10, true);

            _harmony.PatchAll();
            SetupWatcher();
        }

        private void OnDestroy()
        {
            Config.Save();
        }

        private void SetupWatcher()
        {
            FileSystemWatcher watcher = new(Paths.ConfigPath, ConfigFileName);
            watcher.Changed += ReadConfigValues;
            watcher.Created += ReadConfigValues;
            watcher.Renamed += ReadConfigValues;
            watcher.IncludeSubdirectories = true;
            watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            watcher.EnableRaisingEvents = true;
        }

        private void ReadConfigValues(object sender, FileSystemEventArgs e)
        {
            if (!File.Exists(ConfigFileFullPath)) return;
            try
            {
                CCRLogger.LogDebug("ReadConfigValues called");
                Config.Reload();
            }
            catch
            {
                CCRLogger.LogError($"There was an issue loading your {ConfigFileName}");
                CCRLogger.LogError("Please check your config entries for spelling and format!");
            }
        }

        #region ConfigOptions

        private static ConfigEntry<bool>? _serverConfigLocked;

        private ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description,
            bool synchronizedSetting = true)
        {
            ConfigDescription extendedDescription =
                new(
                    description.Description +
                    (synchronizedSetting ? " [Synced with Server]" : " [Not Synced with Server]"),
                    description.AcceptableValues, description.Tags);
            ConfigEntry<T> configEntry = Config.Bind(group, name, value, extendedDescription);
            //var configEntry = Config.Bind(group, name, value, description);

            SyncedConfigEntry<T> syncedConfigEntry = ConfigSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }

        private ConfigEntry<T> config<T>(string group, string name, T value, string description,
            bool synchronizedSetting = true)
        {
            return config(group, name, value, new ConfigDescription(description), synchronizedSetting);
        }

        private class ConfigurationManagerAttributes
        {
            public bool? Browsable = false;
        }

        #endregion


        [HarmonyPatch(typeof(CraftingStation), nameof(CraftingStation.Start))]
        static class CraftingStation_Start_Patch
        {
            static void Postfix(CraftingStation __instance, ref List<CraftingStation> ___m_allStations)
            {
                if (__instance.name is "forge_cart_craftingstation" or "stone_cart_craftingstation"
                    or "workbench_cart_craftingstation")
                {
                    if (___m_allStations.Contains(__instance)) return;
                    ___m_allStations.Add(__instance);
                }
            }
        }

        [HarmonyPatch(typeof(CraftingStation), nameof(CraftingStation.FixedUpdate))]
        static class CCRCraftingStation_FixedUpdate_Patch
        {
            static void Postfix(CraftingStation __instance, ref float ___m_useTimer,
                ref float ___m_updateExtensionTimer, GameObject ___m_inUseObject)
            {
                if (__instance.name is "forge_cart_craftingstation" or "stone_cart_craftingstation"
                    or "workbench_cart_craftingstation")
                {
                    ___m_useTimer += Time.fixedDeltaTime;
                    ___m_updateExtensionTimer += Time.fixedDeltaTime;
                    if (___m_inUseObject) ___m_inUseObject.SetActive(___m_useTimer < 1f);
                }
            }
        }
    }
}