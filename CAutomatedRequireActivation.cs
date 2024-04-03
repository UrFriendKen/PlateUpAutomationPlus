using Unity.Entities;

namespace KitchenAutomationPlus
{
    internal struct CAutomatedRequireActivation : IComponentData
    {
        public bool IsSingleStep;
        public bool IsRequireItem;
        public bool Performed;
    }
}
