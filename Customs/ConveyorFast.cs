using Kitchen;
using KitchenData;
using KitchenLib.Customs;
using KitchenLib.References;
using KitchenLib.Utils;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace KitchenAutomationPlus.Customs
{
    public class ConveyorFast : CustomAppliance
    {
        public override int BaseGameDataObjectID => ApplianceReferences.Grabber;
        public override string UniqueNameID => "beltfast";
        public override GameObject Prefab => Main.Bundle.LoadAsset<GameObject>("Belt - Fast");
        public override List<IApplianceProperty> Properties => new List<IApplianceProperty>()
        {
            new CConveyPushItems()
            {
                Delay = 0.33f,
                Push = true,
                Grab = false,
                GrabSpecificType = false
            },

            new CConveyCooldown()
            {
                Total = 0.01f
            },

            new CItemHolder(),

            new CApplianceGrabPoint()
        };
        public override bool IsNonInteractive => false;
        public override OccupancyLayer Layer => OccupancyLayer.Default;
        public override bool IsPurchasable => false;
        public override bool IsPurchasableAsUpgrade => true;
        public override DecorationType ThemeRequired => DecorationType.Null;
        public override ShoppingTags ShoppingTags => ShoppingTags.Automation;
        public override RarityTier RarityTier => RarityTier.Rare;
        public override PriceTier PriceTier => PriceTier.Medium;
        public override bool StapleWhenMissing => false;
        public override bool SellOnlyAsDuplicate => false;
        public override bool PreventSale => false;
        public override bool IsNonCrated => false;


        public override List<(Locale, ApplianceInfo)> InfoList => new List<(Locale, ApplianceInfo)>
        {
            (Locale.English, new ApplianceInfo()
            {
                Name = "Conveyor - Fast",
                Description = "Automatically moves items, but faster!",
                Sections = new List<Appliance.Section>()
                {
                    new Appliance.Section()
                    {
                        Title = "Reckless Handling",
                        Description = "Moves items at {{+300%}} speed"
                    }
                }
            })
        };

        public override List<Appliance> Upgrades => new List<Appliance>()
        {
            GDOUtils.GetExistingGDO(ApplianceReferences.GrabberRotatable) as Appliance,
            GDOUtils.GetExistingGDO(ApplianceReferences.GrabberSmart) as Appliance
        };

        bool isRegistered = false;

        static FieldInfo pushObject = typeof(ConveyItemsView).GetField("PushObject", BindingFlags.NonPublic | BindingFlags.Instance);
        static FieldInfo smartActive = typeof(ConveyItemsView).GetField("SmartActive", BindingFlags.NonPublic | BindingFlags.Instance);
        static FieldInfo smartInactive = typeof(ConveyItemsView).GetField("SmartInactive", BindingFlags.NonPublic | BindingFlags.Instance);
        static FieldInfo typeContainer = typeof(ConveyItemsView).GetField("TypeContainer", BindingFlags.NonPublic | BindingFlags.Instance);
        static FieldInfo animator = typeof(ConveyItemsView).GetField("Animator", BindingFlags.NonPublic | BindingFlags.Instance);



        public override void OnRegister(Appliance gameDataObject)
        {
            base.OnRegister(gameDataObject);

            if (!isRegistered)
            {
                ApplyMaterials();
                ApplyComponents();
                isRegistered = true;
            }

        }

        private void ApplyMaterials()
        {
            var materials = new Material[1];

            materials[0] = MaterialUtils.GetExistingMaterial("Wood - Default");
            MaterialUtils.ApplyMaterial(Prefab, "Collider", materials);

            materials[0] = MaterialUtils.GetExistingMaterial("Plastic - Dark Grey");
            MaterialUtils.ApplyMaterial(Prefab, "Belt/Belt", materials);

            materials[0] = MaterialUtils.GetExistingMaterial("Lit");
            MaterialUtils.ApplyMaterial(Prefab, "Belt/Camera", materials);
            MaterialUtils.ApplyMaterial(Prefab, "Belt/Camera Light Off", materials);
            MaterialUtils.ApplyMaterial(Prefab, "Belt/Camera Light On", materials);
            MaterialUtils.ApplyMaterial(Prefab, "Belt/Camera Stand", materials);
            MaterialUtils.ApplyMaterial(Prefab, "Belt/Circle", materials);

            materials[0] = MaterialUtils.GetExistingMaterial("Metal- Shiny Blue");
            MaterialUtils.ApplyMaterial(Prefab, "Belt/Circle.001", materials);
            MaterialUtils.ApplyMaterial(Prefab, "Belt/Legs", materials);

            materials[0] = MaterialUtils.GetExistingMaterial("Plastic - Red");
            MaterialUtils.ApplyMaterial(Prefab, "Belt/Marker", materials);
        }

        private void ApplyComponents()
        {
            Prefab.AddComponent<HoldPointContainer>().HoldPoint = GameObjectUtils.GetChildObject(Prefab, "GameObject/HoldPoint").transform;

            ConveyItemsView conveyItemsView = Prefab.AddComponent<ConveyItemsView>();
            pushObject.SetValue(conveyItemsView, GameObjectUtils.GetChildObject(Prefab, "GameObject/HoldPoint"));
            smartActive.SetValue(conveyItemsView, null);
            smartInactive.SetValue(conveyItemsView, null);
            typeContainer.SetValue(conveyItemsView, null);
            animator.SetValue(conveyItemsView, null);
        }
    }
}
