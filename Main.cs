using Kitchen;
using KitchenAutomationPlus.Customs;
using KitchenAutomationPlus.Systems.PseudoProcess;
using KitchenData;
using KitchenLib;
using KitchenLib.Customs;
using KitchenLib.Event;
using KitchenLib.References;
using KitchenLib.Utils;
using KitchenMods;
using PreferenceSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Collections;
using Unity.Entities;
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
        public const string MOD_VERSION = "1.6.11";
        public const string MOD_AUTHOR = "IcedMilo";
        public const string MOD_GAMEVERSION = ">=1.1.5";
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
        public const string VARIABLE_PROVIDER_ALLOW_SWITCH_DURING_NIGHT_ID = "variableProviderAllowSwitchDuringNight";
        public const string SPAWN_DIRTY_PLATES_PRACTICE_ID = "spawnDirtyPlatesPractice";
        public const string TELEPORTER_ALLOW_UNASSIGN_ID = "teleporterAllowUnassign";
        public const string BIN_GRAB_LEVEL_ID = "binGrabLevel";
        public const string DISHWASHER_AUTO_START_ID = "dishwasherAutoStart";
        public const string MICROWAVE_AUTO_START_ID = "microwaveAutoStart";
        public const string PORTIONER_DISALLOW_AUTO_SPLIT_MERGE = "portionerDisallowAutoSplitMerge";
        public const string GAS_OVERRIDE_ALWAYS_ENABLED_ID = "gasOverrideAlwaysEnabled";
        public const string LAZY_MIXER_ENABLED_ID = "lazyMixerEnabled";
        public const string GRABBER_MIXER_ENABLED_ID = "grabberMixerEnabled";
        public const string SMART_GRABBER_ROTATING_ENABLED_ID = "smartGrabberRotatingEnabled";
        public const string CONVEYOR_FAST_ENABLED_ID = "conveyorFastEnabled";
        public const string GRABBER_MERGING_ENABLED_ID = "grabberMergingEnabled";
        public const string GRABBER_DISTRIBUTING_ENABLED_ID = "grabberDistributingEnabled";
        public const string REFILLED_BROTH_CHANGE_ID = "refilledBrothChange";
        public const string CONVEYORMIXER_CAN_TAKE_FOOD_ID = "conveyorMixerCanTakeFood";

        internal static PreferenceSystemManager PrefManager;

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
            List<Appliance> customGrabbers = new List<Appliance>();
            Appliance smartRotatingGrabber = GetModdedGDO<Appliance, SmartRotatingGrabber>();
            if (smartRotatingGrabber != null && PrefManager.Get<bool>(SMART_GRABBER_ROTATING_ENABLED_ID))
                customGrabbers.Add(smartRotatingGrabber);

            Appliance rotatingGrabberMerger = GetModdedGDO<Appliance, RotatingGrabberMerger>();
            if (rotatingGrabberMerger != null && PrefManager.Get<bool>(GRABBER_MERGING_ENABLED_ID))
                customGrabbers.Add(rotatingGrabberMerger);

            Appliance rotatingGrabberDistributor = GetModdedGDO<Appliance, RotatingGrabberDistributor>();
            if (rotatingGrabberDistributor != null && PrefManager.Get<bool>(GRABBER_DISTRIBUTING_ENABLED_ID))
                customGrabbers.Add(rotatingGrabberDistributor);

            Appliance conveyorFast = GetModdedGDO<Appliance, ConveyorFast>();
            Appliance conveyor = GDOUtils.GetExistingGDO(ApplianceReferences.Grabber) as Appliance;
            Appliance grabber = GDOUtils.GetExistingGDO(ApplianceReferences.Belt) as Appliance;
            Appliance rotatingGrabber = GDOUtils.GetExistingGDO(ApplianceReferences.GrabberRotatable) as Appliance;
            Appliance smartGrabber = GDOUtils.GetExistingGDO(ApplianceReferences.GrabberSmart) as Appliance;
            if (customGrabbers.Count > 0)
            {
                if (grabber != null && smartRotatingGrabber != null && smartGrabber != null && rotatingGrabber != null)
                {
                    for (int i = 0; i < customGrabbers.Count; i++)
                    {
                        Main.LogInfo(customGrabbers[i]);
                        grabber.Upgrades.Add(customGrabbers[i]);
                        if (conveyorFast != null)
                        {
                            conveyorFast.Upgrades.Add(customGrabbers[i]);
                        }
                        if (i < customGrabbers.Count - 1)
                        {
                            customGrabbers[i].Upgrades.Remove(rotatingGrabber);
                            customGrabbers[i].Upgrades.Add(customGrabbers[i + 1]);
                        }
                        if (i == 0)
                        {
                            smartGrabber.Upgrades.Add(customGrabbers[i]);
                            smartGrabber.Upgrades.Remove(rotatingGrabber);
                        }
                    }
                }
            }

            Appliance[] baseGameBelts = new Appliance[] { conveyor, grabber, rotatingGrabber, smartGrabber };
            foreach (Appliance appliance in baseGameBelts)
            {
                if ((appliance.Properties?.Count ?? 0) < 1)
                {
                    continue;
                }
                for (int i = 0; i < appliance.Properties.Count; i++)
                {
                    if (appliance.Properties[i] is CConveyCooldown cooldown)
                    {
                        cooldown.Total = 0.01f;
                        appliance.Properties[i] = cooldown;
                    }
                }
            }

            if (conveyorFast != null)
            {
                if (PrefManager.Get<bool>(CONVEYOR_FAST_ENABLED_ID))
                {
                    if (conveyor != null)
                    {
                        conveyor.Upgrades.Add(conveyorFast);
                    }
                }
                else
                {
                    conveyorFast.IsPurchasable = false;
                    conveyorFast.IsPurchasableAsUpgrade = false;
                }
            }

            List<Appliance> customMixers = new List<Appliance>();
            Appliance lazymixer = GetModdedGDO<Appliance, LazyMixer>();
            if (lazymixer != null && PrefManager.Get<bool>(LAZY_MIXER_ENABLED_ID))
                customMixers.Add(lazymixer);

            Appliance grabbermixer = GetModdedGDO<Appliance, GrabberMixer>();
            if (grabbermixer != null && PrefManager.Get<bool>(GRABBER_MIXER_ENABLED_ID))
                customMixers.Add(grabbermixer);

            if (customMixers.Count > 0)
            {
                Appliance mixer = GDOUtils.GetExistingGDO(ApplianceReferences.Mixer) as Appliance;
                Appliance conveyorMixer = GDOUtils.GetExistingGDO(ApplianceReferences.MixerPusher) as Appliance;
                Appliance rapidMixer = GDOUtils.GetExistingGDO(ApplianceReferences.MixerRapid) as Appliance;
                if (mixer != null && conveyorMixer != null && rapidMixer != null)
                {
                    for (int i = 0; i < customMixers.Count; i++)
                    {
                        Main.LogInfo(customMixers[i]);
                        mixer.Upgrades.Add(customMixers[i]);
                        if (i < customMixers.Count - 1)
                        {
                            customMixers[i].Upgrades.Remove(rapidMixer);
                            customMixers[i].Upgrades.Add(customMixers[i + 1]);
                        }
                        if (i == 0)
                        {
                            conveyorMixer.Upgrades.Add(customMixers[i]);
                            conveyorMixer.Upgrades.Remove(rapidMixer);
                        }
                    }
                }
            }


            Appliance dishWasher = GetExistingGDO<Appliance>(ApplianceReferences.DishWasher);
            if (dishWasher != null)
            {
                TryRemoveComponentsFromAppliance<Appliance>(ApplianceReferences.DishWasher, new Type[] { typeof(CChangeProviderAfterDuration), typeof(CChangeProviderWhenEmpty) });

                dishWasher.Properties.Add(new CDynamicItemProvider()
                {
                    StorageFlags = ItemStorage.None
                });

                CDynamicChangeProvider dishWasherDynamicProvider = new CDynamicChangeProvider(
                    new int[] { ItemReferences.PlateDirty, ItemReferences.PlateDirtywithfood, ItemReferences.WokBurned },
                    new int[] { ItemReferences.Plate, ItemReferences.Plate, ItemReferences.Wok });

                int drinksModCupBobaDirty = -551182937;
                int applianceLibCup = -1951140858;
                Main.LogInfo("Attempting to add DrinksMod boba cups to dishwasher DynamicProvider");
                dishWasherDynamicProvider.Add(drinksModCupBobaDirty, applianceLibCup);

                int miniCafeSmallMugDirty = 213976453;
                int miniCafeSmallMug = 1721483158;
                Main.LogInfo("Attempting to add MiniCafe small mugs to dishwasher DynamicProvider");
                dishWasherDynamicProvider.Add(miniCafeSmallMugDirty, miniCafeSmallMug);

                int miniCafeBigMugDirty = 1527576247;
                int miniCafeBigMug = -895445170;
                Main.LogInfo("Attempting to add MiniCafe big mugs to dishwasher DynamicProvider");
                dishWasherDynamicProvider.Add(miniCafeBigMugDirty, miniCafeBigMug);

                dishWasher.Properties.Add(dishWasherDynamicProvider);

                for (int i = 0; i < dishWasher.Properties.Count; i++)
                {
                    if (dishWasher.Properties[i].GetType() == typeof(CTakesDuration))
                    {
                        float baseDuration = ((CTakesDuration)dishWasher.Properties[i]).Total;
                        dishWasher.Properties.Add(new CPseudoProcessDuration(
                            baseDuration,
                            new int[] { ItemReferences.PlateDirtywithfood, ItemReferences.WokBurned },
                            new float[] { baseDuration * 1.5f, baseDuration * 2f }));
                    }
                }
            }


            Appliance gasOverride = GetExistingGDO<Appliance>(ApplianceReferences.GasSafetyOverride);
            if (gasOverride != null)
            {
                gasOverride.EffectCondition = new ActivatePreferenceConditional.CEffectPreferenceConditional()
                {
                    PreferenceID = GAS_OVERRIDE_ALWAYS_ENABLED_ID,
                    ConditionWhenEnabled = ActivatePreferenceConditional.EffectCondition.Always,
                    ConditionWhenDisabled = ActivatePreferenceConditional.EffectCondition.WhileBeingUsed
                };
            }

            Appliance gasLimiter = GetExistingGDO<Appliance>(ApplianceReferences.GasLimiter);
            if (gasOverride != null)
            {
                if (gasLimiter.EffectType is CApplianceSpeedModifier applianceSpeedModifier)
                {
                    applianceSpeedModifier.BadSpeed = float.MinValue;
                    gasLimiter.EffectType = applianceSpeedModifier;
                }
            }
        }

        private void AddGameData()
        {
            LogInfo("Attempting to register game data...");

            AddGameDataObject<SmartRotatingGrabber>();
            AddGameDataObject<LazyMixer>();
            AddGameDataObject<ConveyorFast>();
            AddGameDataObject<GrabberMixer>();
            AddGameDataObject<RotatingGrabberMerger>();
            AddGameDataObject<RotatingGrabberDistributor>();

            LogInfo("Done loading game data.");
        }

        protected override void OnUpdate()
        {
        }

        private void UpdateAppliances()
        {
            if (PrefManager.Get<int>(CONVEYORMIXER_CAN_TAKE_FOOD_ID) == 1)
            {
                Main.LogInfo("Updating conveyor mixer prefab to add CApplianceGrabPoint");
                var conveyorMixer = GDOUtils.GetExistingGDO(ApplianceReferences.MixerPusher) as Appliance;
                if (conveyorMixer != null)
                {
                    conveyorMixer.Properties.Add(new CApplianceGrabPoint());
                    Main.LogInfo("Successfully added CApplianceGrabPoint");
                }
                else
                {
                    Main.LogInfo("Could not find Conveyor Mixer GDO!");
                }
            }
        }

        protected sealed override void OnPostActivate(Mod mod)
        {
            // TODO: Uncomment the following if you have an asset bundle.
            // TODO: Also, make sure to set EnableAssetBundleDeploy to 'true' in your ModName.csproj

            // LogInfo("Attempting to load asset bundle...");
            Bundle = mod.GetPacks<AssetBundleModPack>().SelectMany(e => e.AssetBundles).First();
            LogInfo("Done loading asset bundle.");

            // Register custom GDOs
            AddGameData();

            PrefManager = new PreferenceSystemManager(MOD_GUID, MOD_NAME);
            CreatePreferences();

            // Perform actions when game data is built
            Events.BuildGameDataEvent += delegate (object s, BuildGameDataEventArgs args)
            {
                UpdateAppliances();
                args.gamedata.ProcessesView.Initialise(args.gamedata);
            };
        }

        private void CreatePreferences()
        {
            PrefManager
                .AddLabel("Automation Plus")
                .AddInfo("Changing \"Custom Appliances\" only takes effect upon game restart.")
                .AddSpacer()
                .AddSubmenu("Functions", "Functions")
                    .AddButtonWithConfirm("Make Space Outside", "Destroy ALL appliances outside and consolidate blueprints? You cannot undo this action.", delegate(GenericChoiceDecision decision)
                    {
                        if (Session.CurrentGameNetworkMode == GameNetworkMode.Host && decision == GenericChoiceDecision.Accept)
                        {
                            PreferenceActionController.MakeSpaceOutside();
                        }
                    })
                    .AddSpacer()
                    .AddSpacer()
                .SubmenuDone()
                .AddSubmenu("Custom Appliances", "Custom Appliances")
                    .AddLabel("Custom Appliance Settings")
                    .AddInfo("Disabling prevents Custom Apppliances from showing up. Changed settings only takes effect upon game restart.")
                    .AddSpacer()
                    .AddLabel("Lazy Mixer")
                    .AddOption<bool>(
                        LAZY_MIXER_ENABLED_ID,
                        false,
                        new bool[] { false, true },
                        new string[] { "Disabled", "Enabled" })
                    .AddLabel("Grabber Mixer")
                    .AddOption<bool>(
                        GRABBER_MIXER_ENABLED_ID,
                        false,
                        new bool[] { false, true },
                        new string[] { "Disabled", "Enabled" })
                    .AddLabel("Smart Grabber - Rotating")
                    .AddOption<bool>(
                        SMART_GRABBER_ROTATING_ENABLED_ID,
                        false,
                        new bool[] { false, true },
                        new string[] { "Disabled", "Enabled" })
                    .AddLabel("Conveyor - Fast")
                    .AddOption<bool>(
                        CONVEYOR_FAST_ENABLED_ID,
                        false,
                        new bool[] { false, true },
                        new string[] { "Disabled", "Enabled" })
                    .AddLabel("Grabber - Merging")
                    .AddOption<bool>(
                        GRABBER_MERGING_ENABLED_ID,
                        false,
                        new bool[] { false, true },
                        new string[] { "Disabled", "Enabled" })
                    .AddLabel("Grabber - Distributing")
                    .AddOption<bool>(
                        GRABBER_DISTRIBUTING_ENABLED_ID,
                        false,
                        new bool[] { false, true },
                        new string[] { "Disabled", "Enabled" })
                    .AddSpacer()
                    .AddSpacer()
                .SubmenuDone()
                .AddSubmenu("Modified Appliances", "Modified Appliances")
                    .AddLabel("Modified Appliances Settings")
                    .AddLabel("Auto Grab Trash Bag When")
                    .AddOption<int>(
                        BIN_GRAB_LEVEL_ID,
                        0,
                        new int[] { 0, 1 },
                        new string[] { "Not Empty", "Full" })
                    .AddLabel("Auto Start Dishwasher")
                    .AddOption<int>(
                        DISHWASHER_AUTO_START_ID,
                        0,
                        new int[] { 0, 1 },
                        new string[] { "Never", "When Full" })
                    .AddLabel("Auto Start Microwave")
                    .AddOption<int>(
                        MICROWAVE_AUTO_START_ID,
                        0,
                        new int[] { 0, 1 },
                        new string[] { "Never", "When Has Item" })
                    .AddLabel("Prevent Portioner Combining")
                    .AddOption<bool>(
                        PORTIONER_DISALLOW_AUTO_SPLIT_MERGE,
                        false,
                        new bool[] { false, true },
                        new string[] { "Disabled", "Enabled" })
                    .AddLabel("Take Food From Conveyor Mixer")
                    .AddInfo("Only works for newly placed Conveyor Mixers. Requires game restart.")
                    .AddOption<int>(
                        CONVEYORMIXER_CAN_TAKE_FOOD_ID,
                        0,
                        new int[] { 0, 1 },
                        new string[] { "Disabled", "Enabled" })
                    .AddLabel("Gas Override")
                    .AddOption<bool>(
                        GAS_OVERRIDE_ALWAYS_ENABLED_ID,
                        false,
                        new bool[] { false, true },
                        new string[] { "Hold to Activate", "Always On" })
                    .AddSpacer()
                    .AddSpacer()
                .SubmenuDone()
                .AddSubmenu("Filters/Settings", "filters/settings")
                    .AddLabel("Filters/Settings")
                    .AddSpacer()
                    .AddLabel("Allow Rotate")
                    .AddOption<int>(
                        GRABBER_ALLOW_ROTATE_DURING_DAY_ID,
                        0,
                        new int[] { -1, 0, 1, 2 },
                        new string[] { "Anytime", "In Practice Mode and Day", "In Practice Mode Only", "In Practice Mode and Prep" })
                    .AddLabel("Allow Filter Change")
                    .AddOption<int>(
                        SMART_GRABBER_ALLOW_FILTER_CHANGE_DURING_DAY_ID,
                        0,
                        new int[] { 0, 1, 2 },
                        new string[] { "In Practice Mode and Day", "In Practice Mode Only", "Never" })
                    .AddLabel("Switch Ice Cream Station")
                    .AddOption<bool>(
                        VARIABLE_PROVIDER_ALLOW_SWITCH_DURING_NIGHT_ID,
                        false,
                        new bool[] { false, true },
                        new string[] { "In Practice Mode and Day", "Anytime" })
                    .AddLabel("Spawn Dirty Plates in Practice")
                    .AddOption<bool>(
                        SPAWN_DIRTY_PLATES_PRACTICE_ID,
                        false,
                        new bool[] { false, true },
                        new string[] { "Disabled", "Enabled" })
                    .AddSpacer()
                    .AddSpacer()
                .SubmenuDone()
                .AddSubmenu("Teleporter", "teleporter")
                    .AddLabel("Teleporter Settings")
                    .AddSpacer()
                    .AddLabel("Allow Unassign Teleporter")
                    .AddOption<int>(
                        TELEPORTER_ALLOW_UNASSIGN_ID,
                        -1,
                        new int[] { -1, 0, 1, 2 },
                        new string[] { "Never", "Anytime", "In Practice Mode Only", "In Practice Mode and Prep" })
                    .AddSpacer()
                    .AddSpacer()
                    .SubmenuDone()
                .AddSubmenu("Items", "items")
                    .AddLabel("Item Settings")
                    .AddSpacer()
                    .AddLabel("Refilled Broth")
                    .AddOption<int>(
                        REFILLED_BROTH_CHANGE_ID,
                        0,
                        new int[] { 0, 1 },
                        new string[] { "Remains Unchanged", "Turns Into Regular Broth" })
                    .AddSpacer()
                    .AddSpacer()
                .SubmenuDone()
                .AddSpacer()
                .AddSpacer();

            PrefManager.RegisterMenu(PreferenceSystemManager.MenuType.MainMenu);
            PrefManager.RegisterMenu(PreferenceSystemManager.MenuType.PauseMenu);
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

        private bool TryRemoveComponentsFromAppliance<T>(int id, Type[] componentTypesToRemove) where T : GameDataObject
        {
            T gDO = Find<T>(id);

            if (gDO == null)
            {
                return false;
            }

            bool success = false;
            if (typeof(T) == typeof(Appliance))
            {
                Appliance appliance = (Appliance)Convert.ChangeType(gDO, typeof(Appliance));
                for (int i = appliance.Properties.Count - 1; i > -1; i--)
                {
                    if (componentTypesToRemove.Contains(appliance.Properties[i].GetType()))
                    {
                        appliance.Properties.RemoveAt(i);
                        success = true;
                    }
                }
            }
            else if (typeof(T) == typeof(Item))
            {
                Item item = (Item)Convert.ChangeType(gDO, typeof(Item));
                for (int i = item.Properties.Count - 1; i > -1; i--)
                {
                    if (componentTypesToRemove.Contains(item.Properties[i].GetType()))
                    {
                        item.Properties.RemoveAt(i);
                        success = true;
                    }
                }
            }
            return success;
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
