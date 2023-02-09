using Kitchen;
using KitchenAutomationPlus.Preferences;
using Unity.Collections;
using Unity.Entities;

namespace KitchenAutomationPlus
{
    [UpdateAfter(typeof(InteractionGroup))]
    [UpdateAfter(typeof(GrabItems))]
    public class InhibitConveySpecificTypeChange : GameSystemBase
    {
        EntityQuery ConveyorsQuery;

        protected override void Initialise()
        {
            base.Initialise();
            
            ConveyorsQuery = GetEntityQuery(new QueryHelper()
                                            .All(typeof(CConveyCooldown),
                                                 typeof(CConveyPushItems),
                                                 typeof(CItemHolder),
                                                 typeof(CPosition))
                                            );
            RequireSingletonForUpdate<SIsSpecificTypeInhibitSystemRunning>();
            
        }

        protected override void OnUpdate()
        {
            
            NativeArray<Entity> conveyors = ConveyorsQuery.ToEntityArray(Allocator.Temp);
            if (Has<SPracticeMode>() || Main.PrefManager.Get<int>(Main.SMART_GRABBER_ALLOW_FILTER_CHANGE_DURING_DAY_ID) == 0)
            {
                for (int i = 0; i < conveyors.Length; i++)
                {
                    if (Has<CSpecificType>(conveyors[i]))
                    {
                        EntityManager.RemoveComponent<CSpecificType>(conveyors[i]);
                    }
                }
                return;
            }
            
            NativeArray<CConveyPushItems> cConveyPushItems = ConveyorsQuery.ToComponentDataArray<CConveyPushItems>(Allocator.Temp);
            NativeArray<CItemHolder> cItemHolders = ConveyorsQuery.ToComponentDataArray<CItemHolder>(Allocator.Temp);

            for (int i = 0; i < conveyors.Length; i++)
            {
                if (cConveyPushItems[i].GrabSpecificType && cConveyPushItems[i].SpecificType != 0)
                {
                    CConveyPushItems component = cConveyPushItems[i];
                    CSpecificType component2;
                    if (!Require<CSpecificType>(conveyors[i], out component2))
                    {
                        EntityManager.AddComponent<CSpecificType>(conveyors[i]);
                        component2 = new CSpecificType();
                        component2.SpecificType = cConveyPushItems[i].SpecificType;
                        component2.SpecificComponents = cConveyPushItems[i].SpecificComponents;
                        EntityManager.SetComponentData(conveyors[i], component2);
                    }

                    else if (component.SpecificType != component2.SpecificType)
                    {
                        component.SpecificType = component2.SpecificType;
                        component.SpecificComponents = component2.SpecificComponents;
                        SetComponent(conveyors[i], component);
                    }
                }
            }
            
        }
    }
}
