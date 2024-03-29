﻿using Kitchen;
using KitchenData;
using KitchenLib.Customs;
using KitchenLib.References;
using KitchenLib.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace KitchenAutomationPlus.Customs
{
    public class RotatingGrabberDistributor : CustomAppliance
    {
        public override int BaseGameDataObjectID => ApplianceReferences.GrabberRotatable;
        public override string UniqueNameID => "grabberrotatabledistribute";
        public override GameObject Prefab => Main.Bundle.LoadAsset<GameObject>("Grabber - Directional Copy");
        public override List<IApplianceProperty> Properties => new List<IApplianceProperty>()
        {
            new CAutoConveyRotate()
            {
                AfterGrab = false,
                Primed = false
            },

            new CConveyPushRotatable()
            {
                Target = Orientation.Left
            },

            new CConveyPushItemsReversible()
            {
                Delay = 2f,
                Push = true,
                Grab = true,
                GrabSpecificType = false,
                Reversed = false,
                IgnoreProcessingItems = true
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
        public override bool IsPurchasableAsUpgrade => false;
        public override DecorationType ThemeRequired => DecorationType.Null;
        public override ShoppingTags ShoppingTags => ShoppingTags.Automation;
        public override RarityTier RarityTier => RarityTier.Rare;
        public override PriceTier PriceTier => PriceTier.ExtremelyExpensive;
        public override bool StapleWhenMissing => false;
        public override bool SellOnlyAsDuplicate => false;
        public override bool PreventSale => false;
        public override bool IsNonCrated => false;

        public override List<(Locale, ApplianceInfo)> InfoList => new List<(Locale, ApplianceInfo)>
        {
            (Locale.English, new ApplianceInfo()
            {
                Name = "Grabber - Distributing",
                Description = "Automatically takes items. Rotates after pushing an item.",
                Sections = new List<Appliance.Section>()
                {
                    new Appliance.Section()
                    {
                        Title = "Careful Conveyance",
                        Description = "Moves items at {{-50%}} speed"
                    }
                }
            })
        };

        public override List<Appliance> Upgrades => new List<Appliance>()
        {
            GDOUtils.GetExistingGDO(ApplianceReferences.GrabberRotatable) as Appliance
        };

        bool isRegistered = false;

        public override void OnRegister(Appliance appliance)
        {
            base.OnRegister(appliance);

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
            MaterialUtils.ApplyMaterial(Prefab, "BeltRotating/Fixed/Belt.001", materials);
            MaterialUtils.ApplyMaterial(Prefab, "BeltRotating/Fixed/Circle.004", materials);

            materials[0] = MaterialUtils.GetExistingMaterial("Plastic - Shiny Gold");
            MaterialUtils.ApplyMaterial(Prefab, "BeltRotating/Forward/Bars", materials);
            MaterialUtils.ApplyMaterial(Prefab, "BeltRotating/Legs", materials);

            materials[0] = MaterialUtils.GetExistingMaterial("Metal- Shiny Blue");
            MaterialUtils.ApplyMaterial(Prefab, "BeltRotating/Forward/Centre", materials);
            MaterialUtils.ApplyMaterial(Prefab, "BeltRotating/Sides", materials);

            materials[0] = MaterialUtils.GetExistingMaterial("Plastic - Dark Grey");
            MaterialUtils.ApplyMaterial(Prefab, "BeltRotating/Forward/Belt.003", materials);
            MaterialUtils.ApplyMaterial(Prefab, "BeltRotating/Fixed/Belt.004", materials);

            materials[0] = MaterialUtils.GetExistingMaterial("Plastic - Yellow");
            //materials[0] = MaterialUtils.GetExistingMaterial("Plastic - Orange");
            MaterialUtils.ApplyMaterial(Prefab, "BeltRotating/Fixed/Marker", materials);
            MaterialUtils.ApplyMaterial(Prefab, "BeltRotating/Forward/Marker", materials);
        }

        private void ApplyComponents()
        {
            Prefab.AddComponent<HoldPointContainer>().HoldPoint = GameObjectUtils.GetChildObject(Prefab, "GameObject/HoldPoint").transform;

            ConveyItemsViewReversible conveyItemsView = Prefab.AddComponent<ConveyItemsViewReversible>();
            conveyItemsView.PushObject = GameObjectUtils.GetChildObject(Prefab, "GameObject/HoldPoint");
            conveyItemsView.Animator = Prefab.GetComponent<Animator>();
        }
    }
}
