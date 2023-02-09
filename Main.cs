using Kitchen;
using KitchenAutomationPlus.Customs;
using KitchenAutomationPlus.Preferences;
using KitchenData;
using KitchenLib;
using KitchenLib.Customs;
using KitchenLib.Event;
using KitchenLib.References;
using KitchenLib.Utils;
using KitchenMods;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

// Namespace should have "Kitchen" in the beginning
namespace KitchenAutomationPlus
{
    public class Main : BaseMod, IModSystem
    {
        // GUID must be unique and is recommended to be in reverse domain name notation
        // Mod Name is displayed to the player and listed in the mods menu
        // Mod Version must follow semver notation e.g. "1.2.3"
        public const string MOD_GUID = "IcedMilo.PlateUp.AutomationPlus";
        public const string MOD_NAME = "AutomationPlus";
        public const string MOD_VERSION = "1.2.0";
        public const string MOD_AUTHOR = "IcedMilo";
        public const string MOD_GAMEVERSION = ">=1.1.3";
        // Game version this mod is designed for in semver
        // e.g. ">=1.1.3" current and all future
        // e.g. ">=1.1.3 <=1.2.3" for all from/until

        // Boolean constant whose value depends on whether you built with DEBUG or RELEASE mode, useful for testing
#if DEBUG
        public const bool DEBUG_MODE = true;
#else
        public const bool DEBUG_MODE = false;
#endif

        public static AssetBundle Bundle;

        public const string GRABBER_ALLOW_ROTATE_DURING_DAY_ID = "grabberAllowRotateDuringDay";
        public const string SMART_GRABBER_ALLOW_FILTER_CHANGE_DURING_DAY_ID = "smartGrabberAllowFilterChangeDuringDay";
        public const string TELEPORTER_ALLOW_UNASSIGN_ID = "teleporterAllowUnassign";
        public const string BIN_GRAB_LEVEL_ID = "binGrabLevel";

        public const string LAZY_MIXER_ENABLED_ID = "lazyMixerEnabled";
        public const string SMART_GRABBER_ROTATING_ENABLED_ID = "smartGrabberRotatingEnabled";
        public const string REFILLED_BROTH_CHANGE_ID = "refilledBrothChange";

        internal static PreferencesManager PrefManager;

        public Main() : base(MOD_GUID, MOD_NAME, MOD_AUTHOR, MOD_VERSION, MOD_GAMEVERSION, Assembly.GetExecutingAssembly()) { }

        protected override void OnInitialise()
        {
            LogWarning($"{MOD_GUID} v{MOD_VERSION} in use!");

            try
            {
                World.GetExistingSystem<InteractRotatePush>().Enabled = false;
            }
            catch (NullReferenceException)
            {
                LogInfo("Could not disable system Kitchen.InteractRotatePush!");
            }

            UpdateUpgrades();
        }

        private void UpdateUpgrades()
        {
            if (PrefManager.Get<bool>(SMART_GRABBER_ROTATING_ENABLED_ID))
            {
                Appliance grabber = GDOUtils.GetExistingGDO(ApplianceReferences.Grabber) as Appliance;
                if (grabber != null)
                {
                    grabber.Upgrades.Add(GetModdedGDO<Appliance, SmartRotatingGrabber>());
                }
                Appliance rotatingGrabber = GDOUtils.GetExistingGDO(ApplianceReferences.GrabberRotatable) as Appliance;
                Appliance smartGrabber = GDOUtils.GetExistingGDO(ApplianceReferences.GrabberSmart) as Appliance;
                if (smartGrabber != null && rotatingGrabber != null)
                {
                    smartGrabber.Upgrades.Remove(rotatingGrabber);
                    smartGrabber.Upgrades.Add(GetModdedGDO<Appliance, SmartRotatingGrabber>());
                }
            }

            if (PrefManager.Get<bool>(LAZY_MIXER_ENABLED_ID))
            {
                Appliance mixer = GDOUtils.GetExistingGDO(ApplianceReferences.Mixer) as Appliance;
                if (mixer != null)
                {
                    mixer.Upgrades.Add(GetModdedGDO<Appliance, LazyMixer>());
                }
                Appliance conveyorMixer = GDOUtils.GetExistingGDO(ApplianceReferences.MixerPusher) as Appliance;
                Appliance rapidMixer = GDOUtils.GetExistingGDO(ApplianceReferences.MixerRapid) as Appliance;
                if (conveyorMixer != null && rapidMixer != null)
                {
                    conveyorMixer.Upgrades.Remove(rapidMixer);
                    conveyorMixer.Upgrades.Add(GetModdedGDO<Appliance, LazyMixer>());
                }
            }
        }

        private void AddGameData()
        {
            LogInfo("Attempting to register game data...");

            AddGameDataObject<SmartRotatingGrabber>();
            AddGameDataObject<LazyMixer>();

            LogInfo("Done loading game data.");
        }

        protected override void OnUpdate()
        {
        }

        protected override void OnPostActivate(KitchenMods.Mod mod)
        {
            // TODO: Uncomment the following if you have an asset bundle.
            // TODO: Also, make sure to set EnableAssetBundleDeploy to 'true' in your ModName.csproj

            // LogInfo("Attempting to load asset bundle...");
            Bundle = mod.GetPacks<AssetBundleModPack>().SelectMany(e => e.AssetBundles).First();
            LogInfo("Done loading asset bundle.");

            // Register custom GDOs
            AddGameData();

            // Perform actions when game data is built
            Events.BuildGameDataEvent += delegate (object s, BuildGameDataEventArgs args)
            {
                
            };

            PrefManager = new PreferencesManager(MOD_GUID, MOD_NAME);

            CreatePreferences();
        }

        private void CreatePreferences()
        {
            PrefManager.AddLabel("Automation Plus");
            PrefManager.AddInfo("Changing \"Custom Appliances\" only takes effect upon game restart.");
            PrefManager.AddSpacer();

            PrefManager.AddSubmenu("Custom Appliances", "Custom Appliances");
            PrefManager.AddLabel("Custom Appliance Settings");
            PrefManager.AddInfo("Disabling prevents Custom Apppliances from showing up in upgrades. They will still appear in the shop as upgraded blueprints");
            PrefManager.AddSpacer();
            PrefManager.AddLabel("Lazy Mixer");
            PrefManager.AddOption<bool>(
                LAZY_MIXER_ENABLED_ID,
                "Lazy Mixer",
                false,
                new bool[] { false, true },
                new string[] { "Disabled", "Enabled" });
            PrefManager.AddLabel("Smart Grabber - Rotating");
            PrefManager.AddOption<bool>(
                SMART_GRABBER_ROTATING_ENABLED_ID,
                "Smart Grabber - Rotating",
                false,
                new bool[] { false, true },
                new string[] { "Disabled", "Enabled" });
            PrefManager.AddSpacer();
            PrefManager.AddSpacer();
            PrefManager.SubmenuDone();

            PrefManager.AddSubmenu("Grabber", "grabber");
            PrefManager.AddLabel("Grabber Settings");
            PrefManager.AddSpacer();
            PrefManager.AddLabel("Allow Rotate");
            PrefManager.AddOption<int>(
                GRABBER_ALLOW_ROTATE_DURING_DAY_ID,
                "Allow Rotate",
                0,
                new int[]{ 0, 1, 2 },
                new string[] { "In Practice Mode and Day", "In Practice Mode Only", "In Practice Mode and Prep" });
            PrefManager.AddLabel("Allow Filter Change");
            PrefManager.AddOption<int>(
                SMART_GRABBER_ALLOW_FILTER_CHANGE_DURING_DAY_ID,
                "Allow Filter Change",
                0,
                new int[] { 0, 1 },
                new string[] { "In Practice Mode and Day", "In Practice Mode Only" });
            PrefManager.AddSpacer();
            PrefManager.AddSpacer();
            PrefManager.SubmenuDone();

            PrefManager.AddSubmenu("Teleporter", "teleporter");
            PrefManager.AddLabel("Teleporter Settings");
            PrefManager.AddSpacer();
            PrefManager.AddLabel("Allow Unassign Teleporter");
            PrefManager.AddOption<int>(
                TELEPORTER_ALLOW_UNASSIGN_ID,
                "Allow Unassign Teleporter",
                0,
                new int[] { -1, 0, 1, 2 },
                new string[] { "Never", "Anytime", "In Practice Mode Only", "In Practice Mode and Prep" });
            PrefManager.AddSpacer();
            PrefManager.AddSpacer();
            PrefManager.SubmenuDone();

            PrefManager.AddSubmenu("Bin", "bin");
            PrefManager.AddLabel("Bin Settings");
            PrefManager.AddLabel("Grab Trash Bag When");
            PrefManager.AddOption<int>(
                BIN_GRAB_LEVEL_ID,
                "Grab Trash Bag When",
                0,
                new int[] { 0, 1 },
                new string[] { "Not Empty", "Full" });
            PrefManager.AddSpacer();
            PrefManager.AddSpacer();
            PrefManager.SubmenuDone();

            PrefManager.AddSubmenu("Items", "items");
            PrefManager.AddLabel("Item Settings");
            PrefManager.AddSpacer();
            PrefManager.AddLabel("Refilled Broth");
            PrefManager.AddOption<int>(
                REFILLED_BROTH_CHANGE_ID,
                "Refilled Broth",
                0,
                new int[] { 0, 1 },
                new string[] { "Remains Unchanged", "Turns Into Regular Broth" });
            PrefManager.AddSpacer();
            PrefManager.AddSpacer();
            PrefManager.SubmenuDone();

            PrefManager.AddSpacer();
            PrefManager.AddSpacer();

            PrefManager.RegisterMenu(PreferencesManager.MenuType.MainMenu);
            PrefManager.RegisterMenu(PreferencesManager.MenuType.PauseMenu);
        }

        private static T1 GetModdedGDO<T1, T2>() where T1 : GameDataObject
        {
            return (T1)GDOUtils.GetCustomGameDataObject<T2>().GameDataObject;
        }
        private static T GetExistingGDO<T>(int id) where T : GameDataObject
        {
            return (T)GDOUtils.GetExistingGDO(id);
        }
        internal static T Find<T>(int id) where T : GameDataObject
        {
            return (T)GDOUtils.GetExistingGDO(id) ?? (T)GDOUtils.GetCustomGameDataObject(id)?.GameDataObject;
        }

        internal static T Find<T, C>() where T : GameDataObject where C : CustomGameDataObject
        {
            return GDOUtils.GetCastedGDO<T, C>();
        }

        internal static T Find<T>(string modName, string name) where T : GameDataObject
        {
            return GDOUtils.GetCastedGDO<T>(modName, name);
        }

        #region Logging
        public static void LogInfo(string _log) { Debug.Log($"[{MOD_NAME}] " + _log); }
        public static void LogWarning(string _log) { Debug.LogWarning($"[{MOD_NAME}] " + _log); }
        public static void LogError(string _log) { Debug.LogError($"[{MOD_NAME}] " + _log); }
        public static void LogInfo(object _log) { LogInfo(_log.ToString()); }
        public static void LogWarning(object _log) { LogWarning(_log.ToString()); }
        public static void LogError(object _log) { LogError(_log.ToString()); }
        #endregion
    }
}
