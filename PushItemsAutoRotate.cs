using Kitchen;
using KitchenData;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace KitchenAutomationPlus
{
    [UpdateBefore(typeof(InteractionGroup))]
    [UpdateAfter(typeof(ApplyItemProcesses))]
    [UpdateAfter(typeof(GrabItemsAutoRotate))]
    [UpdateAfter(typeof(TeleportItems))]
    public class PushItemsAutoRotate : GameSystemBase
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
            using NativeArray<CConveyPushItemsReversible> pushes = _grabberQuery.ToComponentDataArray<CConveyPushItemsReversible>(Allocator.Temp);
            using NativeArray<CItemHolder> helds = _grabberQuery.ToComponentDataArray<CItemHolder>(Allocator.Temp);
            using NativeArray<CPosition> positions = _grabberQuery.ToComponentDataArray<CPosition>(Allocator.Temp);

            EntityContext ctx = new EntityContext(EntityManager);
            
            float speed = HasStatus(RestaurantStatus.HalloweenTrickSlowConveyors) ? 0.5f : 1f;
            float dt = Time.DeltaTime;


            for (int i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];
                var cooldown = cooldowns[i];
                var push = pushes[i];
                var held = helds[i];
                var pos = positions[i];
                if (!push.Push || push.State == CConveyPushItems.ConveyState.Grab)
                {
                    continue;
                }
                if (cooldown.Remaining > 0f || !Require(held.HeldItem, out CItem cItem))
                {
                    push.State = CConveyPushItems.ConveyState.None;
                    push.Progress = 0f;
                    continue;
                }
                bool hasPerformed = false;
                bool isPushing = false;

                Orientation pushTo = Orientation.Up;

                if (push.Reversed)
                {
                    pushTo = Orientation.Down;
                }
                else if (Require(entity, out CConveyPushRotatable rotate) && rotate.Target != 0)
                {
                    pushTo = rotate.Target;
                }

                Vector3 vector = pos.Rotation.RotateOrientation(pushTo).ToOffset();
                Entity occupant = GetOccupant(vector + pos);
                if (push.IgnoreProcessingItems && Require(held.HeldItem, out CItemUndergoingProcess component) && !component.IsBad)
			    {
                    isPushing = false;
                }

                else if (CanReach(pos, vector + pos) && !Has<CPreventItemTransfer>(occupant))
			    {
                    if (!hasPerformed && ctx.Require<CItemHolder>(occupant, out var comp2))
                    {
                        bool canPush = false;
                        if (Require(occupant, out CItemHolderFilter cItemHolderFilter))
                        {
                            canPush = !cItemHolderFilter.NoDirectInsertion && cItemHolderFilter.AllowCategory(cItem.Category);
                        }
                        else
                        {
                            canPush = cItem.Category == ItemCategory.Generic;
                        }
                        if (comp2.HeldItem == default && canPush)
                        {
                            hasPerformed = true;
                            if (push.Progress < push.Delay)
                            {
                                push.Progress += speed * dt;
                                isPushing = true;
                            }
                            else
                            {
                                push.Progress = 0f;
                                ctx.UpdateHolder(held.HeldItem, occupant);
                                cooldown.Remaining = cooldown.Total;
                                push.State = CConveyPushItems.ConveyState.None;
                            }
                        }
                    }
                    if (!hasPerformed && Require(occupant, out CItemProvider cItemProvider))
                    {
                        if (!Data.TryGet(cItem, out Item output, warn_if_fail: true))
					    {
                            continue;
                        }
                        bool isDynamicItemProvider = Require(occupant, out CDynamicItemProvider cDynamicItemProvider);
                        bool allowReturns = !cItemProvider.PreventReturns;
                        bool itemMatch = (cItemProvider.ProvidedItem == cItem.ID && cItemProvider.ProvidedComponents.IsEquivalent(cItem.Items)) || (isDynamicItemProvider && cItemProvider.Available == 0);
                        bool isInfiniteOrNotFull = cItemProvider.Maximum <= 0 || cItemProvider.Available != cItemProvider.Maximum;
                        if (allowReturns && itemMatch && isInfiniteOrNotFull)
                        {
                            bool canPush = true;
                            if (cItemProvider.Available == 0 && isDynamicItemProvider)
                            {
                                ItemStorage itemStorageFlags = output.ItemStorageFlags;
                                ItemStorage storageFlags = cDynamicItemProvider.StorageFlags;
                                if (!itemStorageFlags.HasFlag(storageFlags))
                                {
                                    canPush = false;
                                }
                            }
                            if (canPush)
                            {
                                hasPerformed = true;
                                if (push.Progress < push.Delay)
                                {
                                    push.Progress += speed * dt;
                                    isPushing = true;
                                }
                                else
                                {
                                    push.Progress = 0f;
                                    cItemProvider.Available++;
                                    cItemProvider.ProvidedItem = cItem.ID;
                                    cItemProvider.ProvidedComponents = cItem.Items;
                                    ctx.Set(occupant, cItemProvider);
                                    ctx.Destroy(held.HeldItem);
                                    held.HeldItem = default(Entity);
                                    cooldown.Remaining = cooldown.Total;
                                    push.State = CConveyPushItems.ConveyState.None;
                                }
                            }
                        }
                    }
                    if (!hasPerformed && Require(occupant, out CApplianceBin cApplianceBin))
                    {
                        if (cApplianceBin.CurrentAmount < cApplianceBin.Capacity)
                        {
                            if (!Data.TryGet(cItem, out Item itemData, warn_if_fail: true))
                            {
                                continue;
                            }
                            if (!itemData.IsIndisposable && itemData.DisposesTo == null)
                            {
                                hasPerformed = true;
                                if (push.Progress < push.Delay)
                                {
                                    push.Progress += speed * dt;
                                    isPushing = true;
                                }
                                else
                                {
                                    push.Progress = 0f;
                                    ctx.Destroy(held.HeldItem);
                                    held.HeldItem = default(Entity);
                                    cApplianceBin.CurrentAmount++;
                                    ctx.Set(occupant, cApplianceBin);
                                    cooldown.Remaining = cooldown.Total;
                                    push.State = CConveyPushItems.ConveyState.None;
                                }
                            }
                        }
                    }
                }
                if (isPushing)
                {
                    push.State = CConveyPushItems.ConveyState.Push;
                    if (Has<CItemUndergoingProcess>(held.HeldItem))
                    {
                        ctx.Remove<CItemUndergoingProcess>(held.HeldItem);
                    }
                }
                else
                {
                    push.State = CConveyPushItems.ConveyState.None;
                    push.Progress = 0f;
                }
                ctx.Set(entity, push);
                ctx.Set(entity, cooldown);
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
            if (!Require<CItemProvider>(target, out CItemProvider comp))
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
