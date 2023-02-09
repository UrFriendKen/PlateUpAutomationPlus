using Kitchen;
using KitchenAutomationPlus;
using System;
using Unity.Collections;
using Unity.Entities;

namespace KitchenAutomationPlus
{
    [UpdateInGroup(typeof(DestructionGroup))]
    [UpdateAfter(typeof(DestroyItemsOvernight))]
    public class ResetConveySpecificTypeAtNight : NightSystem
    {
        EntityQuery ConveyorsQuery;

        protected override void Initialise()
        {
            base.Initialise();
            ConveyorsQuery = GetEntityQuery(new QueryHelper()
                                            .All(typeof(CSpecificType))
                                            );
        }

        protected override void OnUpdate()
        {
            NativeArray<Entity> conveyors = ConveyorsQuery.ToEntityArray(Allocator.Temp);
            Clear<SIsSpecificTypeInhibitSystemRunning>();
            for (int i = 0; i < conveyors.Length; i++)
            {
                EntityManager.RemoveComponent<CSpecificType>(conveyors[i]);
            }
        }
    }
}
