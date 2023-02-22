using Kitchen;
using KitchenLib.References;
using Unity.Collections;
using Unity.Entities;

namespace KitchenAutomationPlus
{
    internal class AutoStartDishwasher : DaySystem
    {
        EntityQuery ApplianceQuery;

        protected override void Initialise()
        {
            base.Initialise();
            ApplianceQuery = GetEntityQuery(new QueryHelper()
                .All(typeof(CAppliance), typeof(CItemProvider), typeof(CChangeProviderAfterDuration), typeof(CIsInactive)));
        }

        protected override void OnUpdate()
        {
            if (Main.PrefManager.Get<int>(Main.DISHWASHER_AUTO_START_ID) == 1)
            {
                NativeArray<Entity> entities = ApplianceQuery.ToEntityArray(Allocator.Temp);
                foreach (Entity entity in entities)
                {
                    if (Require(entity, out CAppliance appliance) && appliance.ID == ApplianceReferences.DishWasher)
                    {
                        if (Require(entity, out CChangeProviderAfterDuration comp))
                        {
                            if (Require(entity, out CItemProvider provider) && provider.Available >= provider.Maximum && provider.ProvidedItem != comp.ReplaceItem)
                            {
                                Main.LogInfo("Auto-starting Dishwasher");
                                EntityManager.RemoveComponent<CIsInactive>(entity);
                            }
                        }
                    }
                }
                entities.Dispose();
            }

        }

    }
}
