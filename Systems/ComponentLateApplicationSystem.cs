using Unity.Entities;

namespace KitchenAutomationPlus.Systems
{
    [UpdateInGroup(typeof(ComponentEarlyApplicationGroup))]
    public abstract class ComponentLateApplicationSystem : ComponentApplicationSystemBase
    {
    }
}
