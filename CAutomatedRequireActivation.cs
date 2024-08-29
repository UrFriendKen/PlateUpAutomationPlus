using KitchenMods;
using Unity.Entities;

namespace KitchenAutomationPlus
{
    internal struct CAutomatedRequireActivation : IComponentData, IModComponent
    {
        public bool IsSingleStep;
        public bool IsRequireItem;
        public bool Performed;
    }
}
