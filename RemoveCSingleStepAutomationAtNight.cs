using Kitchen;
using Unity.Entities;

namespace KitchenAutomationPlus
{
    public class RemoveCSingleStepAutomationAtNight : RestaurantSystem
    {
        public EntityQuery Appliances;

        protected override void Initialise()
        {
            base.Initialise();
            Appliances = GetEntityQuery(typeof(CAppliance));
        }

        protected override void OnUpdate()
        {
            if (!HasSingleton<SIsDayTime>())
            {
                base.EntityManager.RemoveComponent<CSingleStepAutomation>(Appliances);
            }
        }
    }
}
