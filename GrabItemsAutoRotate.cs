using Kitchen;
using KitchenData;
using KitchenMods;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace KitchenAutomationPlus
{
    public struct CConveyPushItemsReversible : IComponentData, IApplianceProperty, IAttachableProperty, IModComponent
    {
        public float Delay;

        public bool Push;

        public bool Grab;

        public bool Reversed;

        public bool GrabSpecificType;

        public int SpecificType;

        public ItemList SpecificComponents;

        public bool IgnoreProcessingItems;

        public float Progress;

        public CConveyPushItems.ConveyState State;
    }


    [UpdateBefore(typeof(TeleportItems))]
    [UpdateBefore(typeof(GrabItems))]
    [UpdateBefore(typeof(InteractionGroup))]
    [UpdateAfter(typeof(GroupReceiveDrink))]
    [UpdateAfter(typeof(GroupReceiveItem))]
    [UpdateAfter(typeof(GroupReceiveBonus))]
    [UpdateAfter(typeof(ApplyItemProcesses))]
    public class GrabItemsAutoRotate : GameSystemBase
    {
        EntityQuery _grabberQuery;

        protected override void Initialise()
        {
            base.Initialise();
            _grabberQuery = GetEntityQuery(new QueryHelper()
            .All(typeof(CConveyCooldown), typeof(CConveyPushItemsReversible), typeof(CItemHolder), typeof(CPosition))
            .None(typeof(CDisableAutomation)));
        }

        protected override void OnUpdate()
        {
            using NativeArray<Entity> entities = _grabberQuery.ToEntityArray(Allocator.Temp);
            using NativeArray<CConveyCooldown> cooldowns = _grabberQuery.ToComponentDataArray<CConveyCooldown>(Allocator.Temp);
            using NativeArray<CConveyPushItemsReversible> grabs = _grabberQuery.ToComponentDataArray<CConveyPushItemsReversible>(Allocator.Temp);
            using NativeArray<CItemHolder> helds = _grabberQuery.ToComponentDataArray<CItemHolder>(Allocator.Temp);
            using NativeArray<CPosition> positions = _grabberQuery.ToComponentDataArray<CPosition>(Allocator.Temp);

            EntityContext ctx = new EntityContext(EntityManager);
            
            float speed = HasStatus(RestaurantStatus.HalloweenTrickSlowConveyors) ? 0.5f : 1f;
            float dt = Time.DeltaTime;


            for (int i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];
                var cooldown = cooldowns[i];
                var grab = grabs[i];
                var held = helds[i];
                var pos = positions[i];

                if (!grab.Grab || grab.State == CConveyPushItems.ConveyState.Push || cooldown.Remaining > 0f)
                {
                    return;
                }
                if (Require(held.HeldItem, out CItem comp))
                {
                    if (grab.State == CConveyPushItems.ConveyState.Grab)
                    {
                        if (grab.Progress < grab.Delay)
                        {
                            grab.Progress += speed * dt;
                            if (Has<CItemUndergoingProcess>(held.HeldItem))
                            {
                                ctx.Remove<CItemUndergoingProcess>(held.HeldItem);
                            }
                        }
                        else
                        {
                            grab.Progress = 0f;
                            grab.State = CConveyPushItems.ConveyState.None;
                            cooldown.Remaining = cooldown.Total;
                            ctx.Set(entity, cooldown);
                        }
                    }
                    if (grab.GrabSpecificType && grab.State == CConveyPushItems.ConveyState.None)
                    {
                        grab.SpecificType = comp.ID;
                        grab.SpecificComponents = comp.Items;
                    }
                    Set(entity, grab);
                    continue;
                }
                grab.State = CConveyPushItems.ConveyState.None;

                Orientation grabFrom = Orientation.Up;
                if (!grab.Reversed)
                {
                    grabFrom = Orientation.Down;
                }
                else if (Require(entity, out CConveyPushRotatable rotate) && rotate.Target != 0)
                {
                    grabFrom = rotate.Target;
                }

                Vector3 grabFromVector = pos.Rotation.RotateOrientation(grabFrom).ToOffset();

                Entity occupant = GetOccupant(grabFromVector + pos);
                if (CanReach(pos, grabFromVector + pos) && !Has<CPreventItemTransfer>(occupant) && (!Require(occupant, out CConveyPushItemsReversible comp2) || comp2.State == CConveyPushItems.ConveyState.None))
                {
                    if (!AttemptGrabHolder(occupant, ctx, entity, ref grab))
                    {
                        AttemptGrabFromProvider(occupant, ctx, entity, ref grab);
                    }
                }
                ctx.Set(entity, grab);
            }
        }
            
        private bool AttemptGrabHolder(Entity target, EntityContext ctx, Entity e, ref CConveyPushItemsReversible grab)
        {
            if (!Require(target, out CItemHolder comp))
            {
                return false;
            }
            if (comp.HeldItem == default(Entity))
            {
                return false;
            }
            if (Has<CPreventItemTransfer>(target))
            {
                return false;
            }
            if (grab.IgnoreProcessingItems && base.EntityManager.RequireComponent<CItemUndergoingProcess>(comp.HeldItem, out var component) && !component.IsBad)
            {
                return false;
            }
            if (grab.GrabSpecificType && grab.SpecificType != 0 && base.EntityManager.RequireComponent<CItem>(comp.HeldItem, out var component2) && (component2.ID != grab.SpecificType || !component2.Items.IsEquivalent(grab.SpecificComponents)))
            {
                return false;
            }
            ctx.UpdateHolder(comp.HeldItem, e);
            ctx.Remove<CItemUndergoingProcess>(comp.HeldItem);
            grab.Progress = 0f;
            grab.State = CConveyPushItems.ConveyState.Grab;
            return true;
        }

        private bool AttemptGrabFromProvider(Entity target, EntityContext ctx, Entity e, ref CConveyPushItemsReversible grab)
        {
            if (!Require(target, out CItemProvider comp))
            {
                return false;
            }
            if (comp.DirectInsertionOnly)
            {
                return false;
            }
            if (comp.Available == 0 && comp.Maximum != 0)
            {
                return false;
            }
            if (grab.GrabSpecificType && grab.SpecificType != 0 && (comp.ProvidedItem != grab.SpecificType || !comp.ProvidedComponents.IsEquivalent(grab.SpecificComponents)))
            {
                return false;
            }
            if (comp.Maximum > 0)
            {
                comp.Available--;
                ctx.Set(target, comp);
            }
            Entity held_item = ctx.CreateItemGroup(comp.ProvidedItem, comp.ProvidedComponents);
            ctx.UpdateHolder(held_item, e);
            grab.Progress = 0f;
            grab.State = CConveyPushItems.ConveyState.Grab;
            return true;
        }
    }
}
