using Kitchen;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;

namespace KitchenAutomationPlus.Systems
{
    [UpdateInGroup(typeof(ComponentReactionGroup))]
    [UpdateAfter(typeof(ComponentLateApplicationGroup))]
    public abstract class ComponentReactionSystem : ComponentManagementSystem
    {
        protected sealed override void Initialise()
        {
            base.Initialise();
            RequireSingletonForUpdate<SIsDayTime>();
        }
    }
}