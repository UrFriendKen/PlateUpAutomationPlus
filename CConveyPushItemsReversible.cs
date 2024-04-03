using Kitchen;
using KitchenData;
using KitchenMods;
using Unity.Entities;

namespace KitchenAutomationPlus
{
    public struct CConveyPushItemsReversible : IComponentData, IApplianceProperty, IAttachableProperty, IModComponent
    {
        public float Delay;

        public bool Push;

        public bool Grab;

        public bool Reversed;

        public bool GrabSpecificType;

        public int SpecificType;

        public ItemList SpecificComponents;

        public bool IgnoreProcessingItems;

        public float Progress;

        public CConveyPushItems.ConveyState State;
    }
}
