using Unity.Entities;

namespace KitchenAutomationPlus.Systems
{
    [UpdateInGroup(typeof(ComponentEarlyApplicationGroup))]
    public abstract class ComponentEarlyApplicationSystem : ComponentApplicationSystemBase
    {
    }
}
