using Kitchen;
using System.Collections.Generic;
using Unity.Entities;

namespace KitchenAutomationPlus.Systems.Activation
{
    internal class AttachNonpersistentActivationComponents : ComponentInitializationAtStartOfDay
    {
        protected override EntityQuery AssignManagedEntities()
        {
            return GetEntityQuery(typeof(CAppliance));
        }

        protected override void Perform(Entity entity)
        {
            if (Require(entity, out CAppliance appliance))
            {
                if (NonpersistentComponentRegistry.ConditionalActivationDirectory.TryGetValue(appliance.ID, out CConditionalActivation condition))
                {
                    Set(entity, condition);
                }
            }
        }
    }
}
