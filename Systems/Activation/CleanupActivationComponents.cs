using Unity.Entities;

namespace KitchenAutomationPlus.Systems.Activation
{
    internal class CleanupActivationComponents : ComponentCleanupSystemAtStartOfNight
    {
        protected override EntityQuery AssignManagedEntities()
        {
            return GetEntityQuery(typeof(CActivationNonpersistentCleanupMarker));
        }

        protected override void Perform(Entity entity)
        {
            EntityManager.RemoveComponent<CConditionalActivation>(entity);
            EntityManager.RemoveComponent<CInhibitedActivation>(entity);
            EntityManager.RemoveComponent<CWasInhibited>(entity);
            EntityManager.RemoveComponent<CActivationNonpersistentCleanupMarker>(entity);

        }
    }
}
