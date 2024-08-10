using Kitchen;
using KitchenData;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace KitchenAutomationPlus
{
    // Why does this break AutoMop???
    //[UpdateBefore(typeof(InteractionGroup))]
    [UpdateAfter(typeof(ApplyItemProcesses))]
    public class PushItemsReversible : GameSystemBase
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
                    Set(entity, push);
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
                Entity occupant = TileManager.GetOccupant(vector + pos);
                if (push.IgnoreProcessingItems && Require(held.HeldItem, out CItemUndergoingProcess component) && !component.IsBad)
                {
                    isPushing = false;
                }

                else if (TileManager.CanReach(pos, vector + pos) && !Has<CPreventItemTransfer>(occupant))
                {
                    if (!hasPerformed && ctx.Require<CItemProvider>(occupant, out var provider) && provider.AllowRefreshes && provider.Available == 0 &&
                        ctx.Has<CRefreshesProviderQuantity>(held.HeldItem) &&
                        (!ctx.Require<CRefreshesSpecificProvider>(held.HeldItem, out var refreshesSpecificProvider) || refreshesSpecificProvider.Item == provider.ProvidedItem))
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
                            provider.Available = provider.Maximum;
                            Set(occupant, provider);
                            ctx.Destroy(held.HeldItem);
                            held.HeldItem = default(Entity);
                            cooldown.Remaining = cooldown.Total;
                            push.State = CConveyPushItems.ConveyState.None;
                        }
                    }
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
    }
}
