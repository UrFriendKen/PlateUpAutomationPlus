using KitchenData;
using Unity.Entities;

namespace KitchenAutomationPlus
{
    public struct CSpecificType : IComponentData
    {
        public int SpecificType;

        public ItemList SpecificComponents;
    }
}
