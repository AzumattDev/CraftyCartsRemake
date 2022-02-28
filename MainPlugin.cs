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
        public const string ModVersion = "3.0.0";
        public const string ModGUID = "azumatt.CraftyCarts";
        public const string Author = "Azumatt";
        public const string ModName = "CraftyCarts";
        private static CCR instance;
        private readonly Harmony _harmony = new(ModGUID);

        private static string ConfigFileName = ModGUID + ".cfg";
        private static string ConfigFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;

        public static readonly ManualLogSource CCRLogger =
            BepInEx.Logging.Logger.CreateLogSource(ModName);

        private static readonly ConfigSync ConfigSync = new(ModGUID)
            { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion };

        public void Awake()
        {
            _serverConfigLocked = config("General", "Force Server Config", true, "Force Server Config");
            _ = ConfigSync.AddLockingConfigEntry(_serverConfigLocked);

            BuildPiece merchantCart = new("craftycarts", "forge_cart");
            merchantCart.Name.English("Forge Cart");
            merchantCart.Description.English("Mobile Forge");
            merchantCart.RequiredItems.Add("Stone", 4, true);
            merchantCart.RequiredItems.Add("Coal", 4, true);
            merchantCart.RequiredItems.Add("Wood", 10, true);
            merchantCart.RequiredItems.Add("Copper", 6, true);

            BuildPiece stoneCart = new("craftycarts", "stone_cart");
            stoneCart.Name.English("Stonecutter Cart");
            stoneCart.Description.English("Mobile Stone Cutter");
            stoneCart.RequiredItems.Add("Wood", 10, true);
            stoneCart.RequiredItems.Add("Iron", 2, true);
            stoneCart.RequiredItems.Add("Stone", 4, true);

            BuildPiece workbenchCart = new("craftycarts", "workbench_cart");
            workbenchCart.Name.English("Workbench Cart");
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
        private static ConfigEntry<bool>? _modEnabled;

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
    }
}