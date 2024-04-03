using KitchenData;
using KitchenMods;
using Unity.Entities;

namespace KitchenAutomationPlus
{
    public struct CAutoConveyRotate : IComponentData, IApplianceProperty, IAttachableProperty, IModComponent
    {
        public bool AfterGrab;
        public bool Primed;
    }
}
