using Kitchen;
using Unity.Entities;

namespace KitchenAutomationPlus
{
    public class RemoveComponentsAtNight : RestaurantSystem
    {
        public EntityQuery Appliances;

        protected override void Initialise()
        {
            base.Initialise();
            Appliances = GetEntityQuery(new QueryHelper()
                .Any(typeof(CSingleStepAutomation), typeof(CAutomatedRequireActivation)));
        }

        protected override void OnUpdate()
        {
            if (!HasSingleton<SIsDayTime>())
            {
                base.EntityManager.RemoveComponent<CSingleStepAutomation>(Appliances);
                base.EntityManager.RemoveComponent<CAutomatedRequireActivation>(Appliances);
            }
        }
    }
}
