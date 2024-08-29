using KitchenMods;
using Unity.Entities;

namespace KitchenAutomationPlus
{
    public struct CSingleStepAutomation : IComponentData, IModComponent
    {
        public int PreprocessedItemID;
    }
}
