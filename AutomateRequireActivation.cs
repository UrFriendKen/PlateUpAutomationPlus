using Kitchen;
using Unity.Collections;
using Unity.Entities;

namespace KitchenAutomationPlus
{
    internal class AutomateRequireActivation : DaySystem
    {
        EntityQuery ApplianceQuery;

        protected override void Initialise()
        {
            base.Initialise();
            ApplianceQuery = GetEntityQuery(new QueryHelper()
                .All(typeof(CAppliance), typeof(CAutomatedRequireActivation), typeof(CItemHolder), typeof(CIsInactive), typeof(CTakesDuration)));
        }

        protected override void OnUpdate()
        {
            NativeArray<Entity> entities = ApplianceQuery.ToEntityArray(Allocator.Temp);
            foreach (Entity entity in entities)
            {
                if (Require(entity, out CItemHolder holder) && Require(entity, out CAutomatedRequireActivation automatedActivation))
                {
                    if (Require(entity, out CTakesDuration comp) && comp.Active)
                    {
                        continue;
                    }

                    if (automatedActivation.Performed)
                    {
                        if (holder.HeldItem != default && automatedActivation.IsSingleStep)
                        {
                            continue;
                        }
                        automatedActivation.Performed = false;
                        Set(entity, automatedActivation);
                    }
                    else if (Main.PrefManager.Get<int>(Main.MICROWAVE_AUTO_START_ID) == 1 && !automatedActivation.Performed && holder.HeldItem != default)
                    {
                        automatedActivation.Performed = true;
                        Set(entity, automatedActivation);
                        EntityManager.RemoveComponent<CIsInactive>(entity);
                    }
                }
            }
            entities.Dispose();
        }

    }
}
