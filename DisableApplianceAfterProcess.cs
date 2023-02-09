using Kitchen;
using Unity.Collections;
using Unity.Entities;

namespace KitchenAutomationPlus
{

    [UpdateInGroup(typeof(ApplianceProcessReactionGroup))]
    public class DisableApplianceAfterProcess : DaySystem
    {
        EntityQuery entityQuery;

        protected override void Initialise()
        {
            base.Initialise();
            entityQuery = GetEntityQuery(new QueryHelper()
                .All(typeof(CAppliance),
                typeof(CSingleStepAutomation),
                typeof(CItemHolder)));
        }

        protected override void OnUpdate()
        {
            if (entityQuery.IsEmpty)
            {
                return;
            }

            NativeArray<Entity> entities = entityQuery.ToEntityArray(Allocator.Temp);
            foreach (Entity entity in entities)
            {
                if (Require(entity, out CItemHolder holder) && holder.HeldItem != default(Entity) && Require(holder.HeldItem, out CItem item))
                {
                    if (Require(entity, out CSingleStepAutomation singleStep))
                    {
                        if (singleStep.PreprocessedItemID == 0 || singleStep.PreprocessedItemID == item.ID)
                        {
                            singleStep.PreprocessedItemID = item.ID;
                            Set(entity, singleStep);
                        }
                        else
                        {
                            if (Require(holder.HeldItem, out CItemUndergoingProcess process))
                            {
                                Main.LogInfo($"Disabling CSingleStepAutomation process");
                                process.IsBad = true;
                                process.Progress = 0f;
                                Set(holder.HeldItem, process);
                            }
                            Set<CIsInactive>(entity);
                        }
                    }
                }
                else
                {
                    EntityManager.RemoveComponent<CIsInactive>(entity);
                    Set(entity, new CSingleStepAutomation()
                    {
                        PreprocessedItemID = 0
                    });
                }

                
            }
            entities.Dispose();
        }
    }
}
