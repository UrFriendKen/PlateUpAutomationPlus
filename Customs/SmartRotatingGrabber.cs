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
    public class SmartRotatingGrabber : CustomAppliance
    {
        public override int BaseGameDataObjectID => ApplianceReferences.GrabberRotatable;
        public override string UniqueNameID => "smartgrabberrotatable";
        public override GameObject Prefab => Main.Bundle.LoadAsset<GameObject>("Smart Grabber - Directional");
        public override List<IApplianceProperty> Properties => new List<IApplianceProperty>()
        {
            new CConveyPushRotatable()
            {
                Target = Orientation.Up
            },

            new CConveyPushItems()
            {
                Delay = 1f,
                Push = true,
                Grab = true,
                GrabSpecificType = true
            },

            new CConveyCooldown()
            {
                Total = 0f
            },

            new CItemHolder(),

            new CApplianceGrabPoint()
        };
        public override bool IsNonInteractive => false;
        public override OccupancyLayer Layer => OccupancyLayer.Default;
        public override bool IsPurchasable => false;
        public override bool IsPurchasableAsUpgrade => false;
        public override DecorationType ThemeRequired => DecorationType.Null;
        public override ShoppingTags ShoppingTags => ShoppingTags.Automation;
        public override RarityTier RarityTier => RarityTier.Rare;
        public override PriceTier PriceTier => PriceTier.Expensive;
        public override bool StapleWhenMissing => false;
        public override bool SellOnlyAsDuplicate => false;
        public override bool PreventSale => false;
        public override bool IsNonCrated => false;


        public override List<(Locale, ApplianceInfo)> InfoList => new List<(Locale, ApplianceInfo)>
        {
            (Locale.English, new ApplianceInfo()
            {
                Name = "Smart Grabber - Rotating",
                Description = "Automatically takes items. Interact during the day to change output direction"
            })
        };

        public override List<Appliance> Upgrades => new List<Appliance>()
        {
            GDOUtils.GetExistingGDO(ApplianceReferences.GrabberRotatable) as Appliance
        };

        bool isRegistered = false;

        static FieldInfo pushObject = ReflectionUtils.GetField<ConveyItemsView>("PushObject", BindingFlags.NonPublic | BindingFlags.Instance);
        static FieldInfo smartActive = ReflectionUtils.GetField<ConveyItemsView>("SmartActive", BindingFlags.NonPublic | BindingFlags.Instance);
        static FieldInfo smartInactive = ReflectionUtils.GetField<ConveyItemsView>("SmartInactive", BindingFlags.NonPublic | BindingFlags.Instance);
        static FieldInfo typeContainer = ReflectionUtils.GetField<ConveyItemsView>("TypeContainer", BindingFlags.NonPublic | BindingFlags.Instance);
        static FieldInfo animator = ReflectionUtils.GetField<ConveyItemsView>("Animator", BindingFlags.NonPublic | BindingFlags.Instance);

        

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

            materials[0] = MaterialUtils.GetExistingMaterial("Lit");
            MaterialUtils.ApplyMaterial(Prefab, "BeltRotating/Forward/Circle.003", materials);
            MaterialUtils.ApplyMaterial(Prefab, "BeltRotating/Input/Belt.001", materials);
            MaterialUtils.ApplyMaterial(Prefab, "BeltRotating/Input/Circle.004", materials);

            materials[0] = MaterialUtils.GetExistingMaterial("Metal- Shiny Blue");
            MaterialUtils.ApplyMaterial(Prefab, "BeltRotating/Forward/Bars", materials);
            MaterialUtils.ApplyMaterial(Prefab, "BeltRotating/Forward/Centre", materials);
            MaterialUtils.ApplyMaterial(Prefab, "BeltRotating/Legs", materials);
            MaterialUtils.ApplyMaterial(Prefab, "BeltRotating/Sides", materials);
            MaterialUtils.ApplyMaterial(Prefab, "BeltRotating/Camera Stand", materials);

            materials[0] = MaterialUtils.GetExistingMaterial("Plastic - Dark Grey");
            MaterialUtils.ApplyMaterial(Prefab, "BeltRotating/Forward/Belt.003", materials);
            MaterialUtils.ApplyMaterial(Prefab, "BeltRotating/Input/Belt.004", materials);

            materials[0] = MaterialUtils.GetExistingMaterial("Plastic - Yellow");
            MaterialUtils.ApplyMaterial(Prefab, "BeltRotating/Forward/Marker.003", materials);
            MaterialUtils.ApplyMaterial(Prefab, "BeltRotating/Input/Marker.004", materials);

            materials[0] = MaterialUtils.GetExistingMaterial("Plastic - Black");
            MaterialUtils.ApplyMaterial(Prefab, "BeltRotating/Camera", materials);

            materials[0] = MaterialUtils.GetExistingMaterial("Indicator Light");
            MaterialUtils.ApplyMaterial(Prefab, "BeltRotating/Camera Light Off", materials);

            materials[0] = MaterialUtils.GetExistingMaterial("Indicator Light On");
            MaterialUtils.ApplyMaterial(Prefab, "BeltRotating/Camera Light On", materials);
        }

        private void ApplyComponents()
        {
            Prefab.AddComponent<HoldPointContainer>().HoldPoint = GameObjectUtils.GetChildObject(Prefab, "GameObject/HoldPoint").transform;

            ConveyItemsView conveyItemsView = Prefab.AddComponent<ConveyItemsView>();
            pushObject.SetValue(conveyItemsView, GameObjectUtils.GetChildObject(Prefab, "GameObject/HoldPoint"));
            smartActive.SetValue(conveyItemsView, GameObjectUtils.GetChildObject(Prefab, "BeltRotating/Camera Light On"));
            smartInactive.SetValue(conveyItemsView, GameObjectUtils.GetChildObject(Prefab, "BeltRotating/Camera Light Off"));
            typeContainer.SetValue(conveyItemsView, GameObjectUtils.GetChildObject(Prefab, "Container"));
            animator.SetValue(conveyItemsView, Prefab.GetComponent<Animator>());
        }
    }
}
