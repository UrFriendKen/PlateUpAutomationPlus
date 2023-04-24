using Kitchen;
using KitchenData;
using KitchenMods;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace KitchenAutomationPlus
{
    public struct CAutoConveyRotate : IComponentData, IApplianceProperty, IAttachableProperty, IModComponent
    {
        public bool AfterGrab;
        public bool Primed;
    }

    public class AutoRotatePush : GameSystemBase
    {
        EntityQuery _grabberQuery;

        protected override void Initialise()
        {
            base.Initialise();
            _grabberQuery = GetEntityQuery(new QueryHelper()
                .All(typeof(CConveyPushRotatable), typeof(CItemHolder), typeof(CAutoConveyRotate), typeof(CPosition))
                .Any(typeof(CConveyPushItems), typeof(CConveyPushItemsReversible))
                .None(typeof(CDisableAutomation)));
        }

        protected override void OnUpdate()
        {
            using NativeArray<Entity> entities = _grabberQuery.ToEntityArray(Allocator.Temp);
            using NativeArray<CConveyPushRotatable> rotates = _grabberQuery.ToComponentDataArray<CConveyPushRotatable>(Allocator.Temp);
            using NativeArray<CItemHolder> helds = _grabberQuery.ToComponentDataArray<CItemHolder>(Allocator.Temp);
            using NativeArray<CAutoConveyRotate> autoRotates = _grabberQuery.ToComponentDataArray<CAutoConveyRotate>(Allocator.Temp);
            using NativeArray<CPosition> positions = _grabberQuery.ToComponentDataArray<CPosition>(Allocator.Temp);

            for (int i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];
                var rotate = rotates[i];
                var held = helds[i];
                var autoRotate = autoRotates[i];
                var pos = positions[i];

                CConveyPushItems.ConveyState state = CConveyPushItems.ConveyState.None;
                if (Require(entity, out CConveyPushItems push1))
                    state = push1.State;
                else if (Require(entity, out CConveyPushItemsReversible push2))
                    state = push2.State;
                CConveyPushItems.ConveyState resetState = autoRotate.AfterGrab ? CConveyPushItems.ConveyState.Grab : CConveyPushItems.ConveyState.Push;

                Vector3 vector = pos.Rotation.RotateOrientation(rotate.Target).ToOffset();
                bool isRotateGrab = autoRotate.AfterGrab;

                if (state == resetState || !IsValidTarget(pos, pos + vector, held.HeldItem, isRotateGrab))
                {
                    autoRotate.Primed = true;
                }

                if (autoRotate.Primed && state != resetState)
                {
                    rotate.Target = GetNextOrientation(pos, rotate.Target, held.HeldItem, isRotateGrab);
                    Set(entity, rotate);
                    autoRotate.Primed = false;
                }
                Set(entity, autoRotate);
            }
        }

        private bool IsValidTarget(Vector3 from, Vector3 to, Entity heldItem, bool isGrab)
        {
            if (!CanReach(from, to))
                return false;
            Entity occupant = GetOccupant(to);
            if (occupant == default)
                return false;
            if (Has<CPreventItemTransfer>(occupant))
                return false;

            if (!isGrab)
            {
                if (heldItem == default || !Require(heldItem, out CItem cItem))
                {
                    return true;
                }

                if (Require(occupant, out CItemProvider provider))
                {
                    if (provider.Maximum < 1 || provider.Available < provider.Maximum)
                    {
                        if (Require(occupant, out CDynamicItemProvider dynamicProvider))
                        {
                            if (provider.Available == 0 && GameData.Main.TryGet(cItem, out Item item, warn_if_fail: true) &&
                                item.ItemStorageFlags.HasFlag(dynamicProvider.StorageFlags))
                            {
                                return true;
                            }
                        }
                        if (provider.ProvidedItem == cItem)
                        {
                            return true;
                        }
                    }
                }

                if (Has<CItemHolder>(occupant))
                {
                    if (!Require(occupant, out CItemHolderFilter itemHolderFilter) || itemHolderFilter.AllowAny || itemHolderFilter.AllowCategory(cItem.Category))
                    {
                        return true;
                    }
                }

                if (Require(occupant, out CApplianceBin bin))
                {
                    if ((bin.Capacity < 1 || bin.CurrentAmount < bin.Capacity) && GameData.Main.TryGet(cItem, out Item item, warn_if_fail: true) && !item.IsIndisposable && item.DisposesTo == null)
                    {
                        return true;
                    }
                }
            }
            else
            {
                if (Require(occupant, out CItemProvider provider))
                {
                    if (provider.Available > 0 || provider.Maximum < 1)
                    {
                        return true;
                    }
                }

                if (Require(occupant, out CItemHolder holder))
                {
                    if (holder.HeldItem != default)
                        return true;
                }

                if (Require(occupant, out CApplianceBin bin))
                {
                    if (bin.EmptyBinItem != 0)
                    {
                        bool grabWhenFull = Main.PrefManager.Get<int>(Main.BIN_GRAB_LEVEL_ID) == 1;
                        if (!grabWhenFull && bin.CurrentAmount > 0)
                        {
                            return true;
                        }
                        if (grabWhenFull && bin.CurrentAmount >= bin.Capacity)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private Orientation GetNextOrientation(CPosition pos, Orientation orientation, Entity heldItem, bool isGrab)
        {
            for (int i = 0; i < 4; i++)
            {
                orientation = orientation.RotateCW();
                if (orientation == Orientation.Down)
                {
                    continue;
                }

                Vector3 vector = pos.Rotation.RotateOrientation(orientation).ToOffset();
                if (IsValidTarget(pos, pos + vector, heldItem, isGrab))
                {
                    break;
                }
            }
            return orientation;
        }
    }
}
