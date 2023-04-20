using Kitchen;
using KitchenData;
using Unity.Collections;
using Unity.Entities;

namespace KitchenAutomationPlus
{
    [UpdateAfter(typeof(GrabItems))]
    public class GrabFromBin : GameSystemBase
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
                .None(typeof(CDisableAutomation)));
        }

        protected override void OnUpdate()
        {
            NativeArray<Entity> Conveyors = ConveyorsQuery.ToEntityArray(Allocator.Temp);
            float speed = HasStatus(RestaurantStatus.HalloweenTrickSlowConveyors) ? 0.5f : 1f;
            float dt = Time.DeltaTime;
            foreach (Entity entity in Conveyors)
            {
                if (!Require(entity, out CConveyPushItems grab) || !grab.Grab || grab.State == CConveyPushItems.ConveyState.Push)
                {
                    continue;
                }
                if (!Require(entity, out CConveyCooldown cooldown) || cooldown.Remaining > 0f)
                {
                    continue;
                }
                if (!Require(entity, out CItemHolder held) || Has<CItem>(held.HeldItem))
                {
                    continue;
                }
                if (Require(entity, out CPosition pos))
                {
                    Entity occupant = GetOccupant(pos.Forward(-1f) + pos);
                    if (!Require(occupant, out CApplianceBin bin))
                    {
                        continue;
                    }
                    if (bin.EmptyBinItem == 0 || grab.GrabSpecificType && grab.SpecificType != 0 && bin.EmptyBinItem != grab.SpecificType)
                    {
                        continue;
                    }
                    if (!CanReach(pos, pos.Forward(-1f) + pos) || HasComponent<CPreventItemTransfer>(occupant) || bin.SelfEmptyTime > 0)
                    {
                        continue;
                    }
                    if (bin.CurrentAmount > 0 && Main.PrefManager.Get<int>(Main.BIN_GRAB_LEVEL_ID) == 0 ||
                        bin.CurrentAmount >= bin.Capacity && Main.PrefManager.Get<int>(Main.BIN_GRAB_LEVEL_ID) == 1)
                    {
                        Set(EntityManager.CreateEntity(), new CCreateItem
                        {
                            ID = bin.EmptyBinItem,
                            Holder = entity
                        });
                        grab.Progress = 0f;
                        grab.State = CConveyPushItems.ConveyState.Grab;
                        Set(entity, grab);
                        bin.CurrentAmount = 0;
                        Set(occupant, bin);
                    }
                }
            }
            Conveyors.Dispose();
        }
    }

}
