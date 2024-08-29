using KitchenData;
using KitchenMods;
using Unity.Entities;

namespace KitchenAutomationPlus
{
    public struct CSpecificType : IComponentData, IModComponent
    {
        public int SpecificType;

        public ItemList SpecificComponents;
    }
}
