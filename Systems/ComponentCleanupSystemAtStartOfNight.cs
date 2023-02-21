using Kitchen;
using Unity.Entities;

namespace KitchenAutomationPlus.Systems
{
    [UpdateInGroup(typeof(ComponentCleanupGroup))]
    public abstract class ComponentCleanupSystemAtStartOfNight : ComponentManagementSystem
    {
        protected sealed override void Initialise()
        {
            base.Initialise();
            RequireSingletonForUpdate<SIsNightFirstUpdate>();
        }

        protected sealed override void OnUpdate()
        {
            base.OnUpdate();
        }
    }
}
