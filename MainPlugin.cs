using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using LocalizationManager;
using PieceManager;
using UnityEngine;
using ServerSync;
using TMPro;
using UnityEngine.UI;

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
        public const string ModVersion = "3.1.8";
        public const string ModGUID = "Azumatt.CraftyCarts";
        public const string Author = "Azumatt";
        public const string ModName = "CraftyCarts";
        private readonly Harmony _harmony = new(ModGUID);
        internal static string ConnectionError = "";

        internal static BuildPiece ForgeCart = null!;
        internal static BuildPiece StoneCart = null!;
        internal static BuildPiece WorkbenchCart = null!;
        internal static BuildPiece CauldronCart = null!;
        internal static BuildPiece BlackForgeCart = null!;
        internal static BuildPiece ArtisanCart = null!;
        internal static BuildPiece UpgradeCart = null!;

        internal const string ForgeCartFabName = "forge_cart";
        internal const string StoneCartFabName = "stone_cart";
        internal const string WorkbenchCartFabName = "workbench_cart";
        internal const string CauldronCartFabName = "cauldron_cart";
        internal const string BlackForgeCartFabName = "blackforge_cart";
        internal const string ArtisanCartFabName = "artisan_cart";
        internal const string PieceUpgradeCartFabName = "piece_upgradecart";

        internal const string ForgeCartInstance = $"{ForgeCartFabName}_craftingstation";
        internal const string StoneCartInstance = $"{StoneCartFabName}_craftingstation";
        internal const string WorkbenchCartInstance = $"{WorkbenchCartFabName}_craftingstation";
        internal const string CauldronCartInstance = $"{CauldronCartFabName}_craftingstation";
        internal const string BlackForgeCartInstance = $"{BlackForgeCartFabName}_craftingstation";
        internal const string ArtisanCartInstance = $"{ArtisanCartFabName}_craftingstation";


        private const string ConfigFileName = ModGUID + ".cfg";
        private static readonly string ConfigFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;

        public static readonly ManualLogSource CCRLogger = BepInEx.Logging.Logger.CreateLogSource(ModName);

        private static readonly ConfigSync ConfigSync = new(ModGUID) { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion };

        public enum Toggle
        {
            On = 1,
            Off = 0,
        }

        public void Awake()
        {
            ConfigSync.IsLocked = true;
            Localizer.Load();

            UpgradeCart = new BuildPiece("craftycartsremake", PieceUpgradeCartFabName);
            UpgradeCart.Category.Set("Crafty Carts");

            ForgeCart = new BuildPiece("craftycartsremake", ForgeCartFabName);
            ForgeCart.RequiredItems.Add("Stone", 4, true);
            ForgeCart.RequiredItems.Add("Coal", 4, true);
            ForgeCart.RequiredItems.Add("Wood", 10, true);
            ForgeCart.RequiredItems.Add("Copper", 6, true);
            ForgeCart.Category.Set("Crafty Carts");
            MaterialReplacer.RegisterGameObjectForMatSwap(Utils.FindChild(ForgeCart.Prefab.transform, "new").gameObject);
            MaterialReplacer.RegisterGameObjectForMatSwap(Utils.FindChild(ForgeCart.Prefab.transform, "Wheel1").gameObject);
            MaterialReplacer.RegisterGameObjectForMatSwap(Utils.FindChild(ForgeCart.Prefab.transform, "Wheel2").gameObject);
            MaterialReplacer.RegisterGameObjectForMatSwap(Utils.FindChild(ForgeCart.Prefab.transform, "UpgradeVisuals").gameObject);

            CauldronCart = new BuildPiece("craftycartsremake", CauldronCartFabName);
            CauldronCart.RequiredItems.Add("Tin", 10, true);
            CauldronCart.RequiredItems.Add("Wood", 10, true);
            CauldronCart.Category.Set("Crafty Carts");
            MaterialReplacer.RegisterGameObjectForMatSwap(Utils.FindChild(CauldronCart.Prefab.transform, "Cauldron_cart").gameObject);
            MaterialReplacer.RegisterGameObjectForMatSwap(Utils.FindChild(CauldronCart.Prefab.transform, "Wheel1").gameObject);
            MaterialReplacer.RegisterGameObjectForMatSwap(Utils.FindChild(CauldronCart.Prefab.transform, "Wheel2").gameObject);

            MaterialReplacer.RegisterGameObjectForMatSwap(Utils.FindChild(CauldronCart.Prefab.transform, "Waterplane").gameObject);
            MaterialReplacer.RegisterGameObjectForMatSwap(Utils.FindChild(CauldronCart.Prefab.transform, "bubbles").gameObject);
            MaterialReplacer.RegisterGameObjectForMatSwap(Utils.FindChild(CauldronCart.Prefab.transform, "steam").gameObject);
            MaterialReplacer.RegisterGameObjectForMatSwap(Utils.FindChild(CauldronCart.Prefab.transform, "UpgradeVisuals").gameObject);

            BlackForgeCart = new BuildPiece("craftycartsremake", BlackForgeCartFabName);
            BlackForgeCart.RequiredItems.Add("BlackMarble", 10, true);
            BlackForgeCart.RequiredItems.Add("YggdrasilWood", 10, true);
            BlackForgeCart.RequiredItems.Add("BlackCore", 5, true);
            BlackForgeCart.Category.Set("Crafty Carts");
            MaterialReplacer.RegisterGameObjectForMatSwap(Utils.FindChild(BlackForgeCart.Prefab.transform, "new").gameObject);
            MaterialReplacer.RegisterGameObjectForMatSwap(Utils.FindChild(BlackForgeCart.Prefab.transform, "Wheel1").gameObject);
            MaterialReplacer.RegisterGameObjectForMatSwap(Utils.FindChild(BlackForgeCart.Prefab.transform, "Wheel2").gameObject);
            MaterialReplacer.RegisterGameObjectForMatSwap(Utils.FindChild(BlackForgeCart.Prefab.transform, "flames").gameObject);
            MaterialReplacer.RegisterGameObjectForMatSwap(Utils.FindChild(BlackForgeCart.Prefab.transform, "Magic Trails").gameObject);
            MaterialReplacer.RegisterGameObjectForMatSwap(Utils.FindChild(BlackForgeCart.Prefab.transform, "UpgradeVisuals").gameObject);


            StoneCart = new BuildPiece("craftycartsremake", StoneCartFabName);
            StoneCart.RequiredItems.Add("Wood", 10, true);
            StoneCart.RequiredItems.Add("Iron", 2, true);
            StoneCart.RequiredItems.Add("Stone", 4, true);
            StoneCart.Category.Set("Crafty Carts");
            MaterialReplacer.RegisterGameObjectForMatSwap(Utils.FindChild(StoneCart.Prefab.transform, "new").gameObject);
            MaterialReplacer.RegisterGameObjectForMatSwap(Utils.FindChild(StoneCart.Prefab.transform, "Wheel1").gameObject);
            MaterialReplacer.RegisterGameObjectForMatSwap(Utils.FindChild(StoneCart.Prefab.transform, "Wheel2").gameObject);

            WorkbenchCart = new BuildPiece("craftycartsremake", WorkbenchCartFabName);
            WorkbenchCart.RequiredItems.Add("Wood", 10, true);
            WorkbenchCart.Category.Set("Crafty Carts");
            MaterialReplacer.RegisterGameObjectForMatSwap(Utils.FindChild(WorkbenchCart.Prefab.transform, "UpgradeVisuals").gameObject);
            MaterialReplacer.RegisterGameObjectForMatSwap(Utils.FindChild(WorkbenchCart.Prefab.transform, "Workbench_cart").gameObject);
            MaterialReplacer.RegisterGameObjectForMatSwap(Utils.FindChild(WorkbenchCart.Prefab.transform, "Wheel1").gameObject);
            MaterialReplacer.RegisterGameObjectForMatSwap(Utils.FindChild(WorkbenchCart.Prefab.transform, "Wheel2").gameObject);

            ArtisanCart = new BuildPiece("craftycartsremake", ArtisanCartFabName);
            ArtisanCart.RequiredItems.Add("Wood", 10, true);
            ArtisanCart.RequiredItems.Add("DragonTear", 2, true);
            ArtisanCart.Category.Set("Crafty Carts");
            MaterialReplacer.RegisterGameObjectForMatSwap(Utils.FindChild(ArtisanCart.Prefab.transform, "artisan_cart_craftingstation").gameObject);
            MaterialReplacer.RegisterGameObjectForMatSwap(Utils.FindChild(ArtisanCart.Prefab.transform, "UpgradeVisuals").gameObject);
            MaterialReplacer.RegisterGameObjectForMatSwap(Utils.FindChild(ArtisanCart.Prefab.transform, "new").gameObject);
            MaterialReplacer.RegisterGameObjectForMatSwap(Utils.FindChild(ArtisanCart.Prefab.transform, "Wheel1").gameObject);
            MaterialReplacer.RegisterGameObjectForMatSwap(Utils.FindChild(ArtisanCart.Prefab.transform, "Wheel2").gameObject);

            UseBumperSticker = config("1 - General", "Use Bumper Sticker", Toggle.On, "Use the bumper sticker on the cart. This is a sign that can be edited, shown on the back of the cart.");
            UseBumperSticker.SettingChanged += ToggleBumperSticker;


            ForgeCartRow = config("Forge Cart", "Inventory Rows", 5, new ConfigDescription("Rows for Forge", new AcceptableValueRange<int>(2, 30)));
            ForgeCartCol = config("Forge Cart", "Inventory Columns", 8, new ConfigDescription("Columns for Forge", new AcceptableValueRange<int>(2, 8)));

            WorkbenchCartRow = config("Workbench Cart", "Inventory Rows", 5, new ConfigDescription("Rows for Workbench", new AcceptableValueRange<int>(2, 30)));

            WorkbenchCartCol = config("Workbench Cart", "Inventory Columns", 8, new ConfigDescription("Columns for Workbench", new AcceptableValueRange<int>(2, 8)));


            StonecutterCartRow = config("Stonecutter Cart", "Inventory Rows", 5, new ConfigDescription("Rows for Stonecutter", new AcceptableValueRange<int>(2, 30)));
            StonecutterCartCol = config("Stonecutter Cart", "Inventory Columns", 8, new ConfigDescription("Columns for Stonecutter", new AcceptableValueRange<int>(2, 8)));

            CauldronCartRow = config("Cauldron Cart", "Inventory Rows", 5, new ConfigDescription("Rows for Cauldron", new AcceptableValueRange<int>(2, 30)));
            CauldronCartCol = config("Cauldron Cart", "Inventory Columns", 8, new ConfigDescription("Columns for Cauldron", new AcceptableValueRange<int>(2, 8)));

            BlackForgeCartRow = config("Black Forge Cart", "Inventory Rows", 5, new ConfigDescription("Rows for Black Forge", new AcceptableValueRange<int>(2, 30)));
            BlackForgeCartCol = config("Black Forge Cart", "Inventory Columns", 8, new ConfigDescription("Columns for Black Forge", new AcceptableValueRange<int>(2, 8)));

            ArtisanCartRow = config("Artisan Table Cart", "Inventory Rows", 5, new ConfigDescription("Rows for Artisan Table", new AcceptableValueRange<int>(2, 30)));
            ArtisanCartCol = config("Artisan Table Cart", "Inventory Columns", 8, new ConfigDescription("Columns for Artisan Table", new AcceptableValueRange<int>(2, 8)));

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

        private static void ToggleBumperSticker(object sender, EventArgs e)
        {
            if (UseBumperSticker.Value.IsOn())
            {
                if (Vagon.m_instances == null) return;
                foreach (Vagon? cart in Vagon.m_instances.Where(x => x.gameObject.GetComponent<CraftyCart>() != null))
                {
                    cart.gameObject.GetComponent<CraftyCart>().bumperSticker.gameObject.SetActive(true);
                }
            }
            else
            {
                if (Vagon.m_instances == null) return;
                foreach (Vagon? cart in Vagon.m_instances.Where(x => x.gameObject.GetComponent<CraftyCart>() != null))
                {
                    cart.gameObject.GetComponent<CraftyCart>().bumperSticker.gameObject.SetActive(false);
                }
            }
        }

        #region ConfigOptions

        public static ConfigEntry<Toggle> UseBumperSticker = null!;
        public static ConfigEntry<int> ForgeCartRow = null!;
        public static ConfigEntry<int> ForgeCartCol = null!;
        public static ConfigEntry<int> StonecutterCartRow = null!;
        public static ConfigEntry<int> StonecutterCartCol = null!;
        public static ConfigEntry<int> WorkbenchCartRow = null!;
        public static ConfigEntry<int> WorkbenchCartCol = null!;
        public static ConfigEntry<int> CauldronCartRow = null!;
        public static ConfigEntry<int> CauldronCartCol = null!;
        public static ConfigEntry<int> BlackForgeCartRow = null!;
        public static ConfigEntry<int> BlackForgeCartCol = null!;
        public static ConfigEntry<int> ArtisanCartRow = null!;
        public static ConfigEntry<int> ArtisanCartCol = null!;

        private ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description, bool synchronizedSetting = true)
        {
            ConfigDescription extendedDescription = new(description.Description + (synchronizedSetting ? " [Synced with Server]" : " [Not Synced with Server]"), description.AcceptableValues, description.Tags);
            ConfigEntry<T> configEntry = Config.Bind(group, name, value, extendedDescription);
            //var configEntry = Config.Bind(group, name, value, description);

            SyncedConfigEntry<T> syncedConfigEntry = ConfigSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }

        private ConfigEntry<T> config<T>(string group, string name, T value, string description, bool synchronizedSetting = true)
        {
            return config(group, name, value, new ConfigDescription(description), synchronizedSetting);
        }

        #endregion
    }

    public static class ToggleExtensions
    {
        public static bool IsOn(this CCR.Toggle toggle) => toggle == CCR.Toggle.On;

        public static bool IsOff(this CCR.Toggle toggle) => toggle == CCR.Toggle.Off;
    }
}