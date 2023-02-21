using Kitchen;
using Unity.Entities;

namespace KitchenAutomationPlus.Systems.Activation
{
    internal class AutomatedActivation : ComponentReactionSystem
    {
        protected override EntityQuery AssignManagedEntities()
        {
            return GetEntityQuery(typeof(CPerformAutomatedActivation));
        }

        protected override void Perform(Entity entity)
        {
            EntityManager.RemoveComponent<CIsInactive>(entity);
        }
    }
}
