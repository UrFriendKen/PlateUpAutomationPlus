using Kitchen;
using Unity.Entities;

namespace KitchenAutomationPlus.Systems
{
    [UpdateInGroup(typeof(ComponentInitializationGroup))]
    public abstract class ComponentInitializationAtStartOfDay : ComponentManagementSystem
    {
        protected sealed override void Initialise()
        {
            base.Initialise();
            RequireSingletonForUpdate<SIsDayFirstUpdate>();
        }

        protected sealed override void OnUpdate()
        {
            base.OnUpdate();
        }
    }
}
