using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;

namespace KitchenAutomationPlus.Systems
{
    [UpdateInGroup(typeof(ComponentApplicationGroup))]
    [UpdateAfter(typeof(ComponentEarlyApplicationGroup))]
    [UpdateBefore(typeof(ComponentLateApplicationGroup))]
    public abstract class ComponentApplicationSystem : ComponentApplicationSystemBase
    {
    }
}
