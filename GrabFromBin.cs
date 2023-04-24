using Kitchen;
using KitchenData;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace KitchenAutomationPlus
{
    [UpdateAfter(typeof(GrabItems))]
    [UpdateAfter(typeof(GrabItemsReversible))]
    public class GrabFromBin : GameSystemBase
    {
        EntityQuery ConveyorsQuery;

        protected override void Initialise()
        {
            base.Initialise();
            ConveyorsQuery = GetEntityQuery(new QueryHelper()
                .All(typeof(CConveyCooldown),
                     typeof(CItemHolder),
                     typeof(CPosition))
                .Any(typeof(CConveyPushItems), typeof(CConveyPushItemsReversible))
                .None(typeof(CDisableAutomation)));
        }

        protected override void OnUpdate()
        {
            NativeArray<Entity> Conveyors = ConveyorsQuery.ToEntityArray(Allocator.Temp);
            float speed = HasStatus(RestaurantStatus.HalloweenTrickSlowConveyors) ? 0.5f : 1f;
            float dt = Time.DeltaTime;
            foreach (Entity entity in Conveyors)
            {
                bool isGrab = false;
                CConveyPushItems.ConveyState state = CConveyPushItems.ConveyState.None;
                bool reversed = false;
                bool isReversiblePush = false;
                if (Require(entity, out CConveyPushItems grab))
                {
                    isGrab = grab.Grab;
                    state = grab.State;
                }
                if (Require(entity, out CConveyPushItemsReversible grabReversible))
                {
                    isReversiblePush = true;
                    isGrab = grabReversible.Grab;
                    state = grabReversible.State;
                    reversed = grabReversible.Reversed;
                }

                if (!isGrab || state == CConveyPushItems.ConveyState.Push)
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
                    Orientation grabFrom = Orientation.Up;
                    if (!reversed)
                    {
                        grabFrom = Orientation.Down;
                    }
                    else if (Require(entity, out CConveyPushRotatable rotate))
                    {
                        grabFrom = rotate.Target;
                    }

                    Vector3 vector = pos.Rotation.RotateOrientation(grabFrom).ToOffset();
                    Entity occupant = GetOccupant(pos + vector);
                    if (!Require(occupant, out CApplianceBin bin))
                    {
                        continue;
                    }
                    if (bin.EmptyBinItem == 0 || grab.GrabSpecificType && grab.SpecificType != 0 && bin.EmptyBinItem != grab.SpecificType)
                    {
                        continue;
                    }
                    if (!CanReach(pos, pos + vector) || HasComponent<CPreventItemTransfer>(occupant) || bin.SelfEmptyTime > 0)
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
                        if (!isReversiblePush)
                        {
                            grab.Progress = 0f;
                            grab.State = CConveyPushItems.ConveyState.Grab;
                            Set(entity, grab);
                        }
                        else
                        {
                            grabReversible.Progress = 0f;
                            grabReversible.State = CConveyPushItems.ConveyState.Grab;
                            Set(entity, grabReversible);
                        }
                        bin.CurrentAmount = 0;
                        Set(occupant, bin);
                    }
                }
            }
            Conveyors.Dispose();
        }
    }

}
