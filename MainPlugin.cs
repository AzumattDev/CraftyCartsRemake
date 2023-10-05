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
        public const string ModVersion = "3.1.1";
        public const string ModGUID = "Azumatt.CraftyCarts";
        public const string Author = "Azumatt";
        public const string ModName = "CraftyCarts";
        private readonly Harmony _harmony = new(ModGUID);
        internal static string ConnectionError = "";

        internal static BuildPiece ForgeCart = null!;
        internal static BuildPiece StoneCart = null!;
        internal static BuildPiece WorkbenchCart = null!;
        
        internal const string ForgeCartFabName = "forge_cart";
        internal const string StoneCartFabName = "stone_cart";
        internal const string WorkbenchCartFabName = "workbench_cart";
        
        internal const string ForgeCartInstance = $"{ForgeCartFabName}_craftingstation";
        internal const string StoneCartInstance = $"{StoneCartFabName}_craftingstation";
        internal const string WorkbenchCartInstance = $"{WorkbenchCartFabName}_craftingstation";


        private const string ConfigFileName = ModGUID + ".cfg";
        private static readonly string ConfigFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;

        public static readonly ManualLogSource CCRLogger = BepInEx.Logging.Logger.CreateLogSource(ModName);

        private static readonly ConfigSync ConfigSync = new(ModGUID) { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion };

        public void Awake()
        {
            ConfigSync.IsLocked = true;

            ForgeCart = new BuildPiece("craftycarts", ForgeCartFabName);
            ForgeCart.Description.English("Mobile Forge");
            ForgeCart.RequiredItems.Add("Stone", 4, true);
            ForgeCart.RequiredItems.Add("Coal", 4, true);
            ForgeCart.RequiredItems.Add("Wood", 10, true);
            ForgeCart.RequiredItems.Add("Copper", 6, true);
            ForgeCart.Category.Set("Crafty Carts");

            StoneCart = new BuildPiece("craftycarts", StoneCartFabName);
            StoneCart.Description.English("Mobile Stone Cutter");
            StoneCart.RequiredItems.Add("Wood", 10, true);
            StoneCart.RequiredItems.Add("Iron", 2, true);
            StoneCart.RequiredItems.Add("Stone", 4, true);
            StoneCart.Category.Set("Crafty Carts");

            WorkbenchCart = new BuildPiece("craftycarts", WorkbenchCartFabName);
            WorkbenchCart.Description.English("Mobile Workbench");
            WorkbenchCart.RequiredItems.Add("Wood", 10, true);
            WorkbenchCart.Category.Set("Crafty Carts");

            ForgeCartRow = config("Forge Cart", "Inventory Rows", 5, new ConfigDescription("Rows for Forge", new AcceptableValueRange<int>(2, 30)));
            ForgeCartCol = config("Forge Cart", "Inventory Columns", 8, new ConfigDescription("Columns for Forge", new AcceptableValueRange<int>(2, 8)));

            WorkbenchCartRow = config("Workbench Cart", "Inventory Rows", 5, new ConfigDescription("Rows for Workbench", new AcceptableValueRange<int>(2, 30)));
            WorkbenchCartCol = config("Workbench Cart", "Inventory Columns", 8, new ConfigDescription("Columns for Workbench", new AcceptableValueRange<int>(2, 8)));

            StonecutterCartRow = config("Stonecutter Cart", "Inventory Rows", 5, new ConfigDescription("Rows for Stonecutter", new AcceptableValueRange<int>(2, 30)));
            StonecutterCartCol = config("Stonecutter Cart", "Inventory Columns", 8, new ConfigDescription("Columns for Stonecutter", new AcceptableValueRange<int>(2, 8)));

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

        public static ConfigEntry<int> ForgeCartRow = null!;
        public static ConfigEntry<int> ForgeCartCol = null!;
        public static ConfigEntry<int> StonecutterCartRow = null!;
        public static ConfigEntry<int> StonecutterCartCol = null!;
        public static ConfigEntry<int> WorkbenchCartRow = null!;
        public static ConfigEntry<int> WorkbenchCartCol = null!;

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
                if (__instance.name is not (ForgeCartInstance or StoneCartInstance or WorkbenchCartInstance)) return;
                if (___m_allStations.Contains(__instance)) return;
                ___m_allStations.Add(__instance);
            }
        }

        [HarmonyPatch(typeof(CraftingStation), nameof(CraftingStation.FixedUpdate))]
        static class CCRCraftingStation_FixedUpdate_Patch
        {
            static void Postfix(CraftingStation __instance, ref float ___m_useTimer,
                ref float ___m_updateExtensionTimer, GameObject ___m_inUseObject)
            {
                if (__instance.name is not (ForgeCartInstance or StoneCartInstance or WorkbenchCartInstance)) return;
                ___m_useTimer += Time.fixedDeltaTime;
                ___m_updateExtensionTimer += Time.fixedDeltaTime;
                if (___m_inUseObject) ___m_inUseObject.SetActive(___m_useTimer < 1f);
            }
        }

        [HarmonyPatch(typeof(Container), nameof(Container.Awake))]
        static class Container_Awake_Patch
        {
            static void Postfix(Container __instance, ref Inventory ___m_inventory)
            {
                if (__instance.m_nview.GetZDO() == null)
                    return;
                string cartName = __instance.transform.root.name.Replace("(Clone)", "").Trim();

                ref int inventoryColumns = ref ___m_inventory.m_width;
                ref int inventoryRows = ref ___m_inventory.m_height;
                switch (cartName)
                {
                    // Forge Cart Storage
                    case ForgeCartFabName:
                        inventoryRows = ForgeCartRow.Value;
                        inventoryColumns = ForgeCartCol.Value;
                        break;
                    // Stonecutter Cart Storage
                    case StoneCartFabName:
                        inventoryRows = StonecutterCartRow.Value;
                        inventoryColumns = StonecutterCartCol.Value;
                        break;
                    // Workbench Cart Storage
                    case WorkbenchCartFabName:
                        inventoryRows = WorkbenchCartRow.Value;
                        inventoryColumns = WorkbenchCartCol.Value;
                        break;
                }
            }
        }
    }
}