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
                if (state == resetState || !IsValidTarget(pos, pos + vector))
                {
                    autoRotate.Primed = true;
                }

                if (autoRotate.Primed && state != resetState)
                {
                    rotate.Target = rotate.Target.RotateCW();
                    if (rotate.Target == Orientation.Down)
                    {
                        rotate.Target = Orientation.Left;
                    }
                    Set(entity, rotate);

                    vector = pos.Rotation.RotateOrientation(rotate.Target).ToOffset();
                    if (IsValidTarget(pos, pos + vector))
                    {
                        autoRotate.Primed = false;
                    }
                }
                Set(entity, autoRotate);
            }
        }

        private bool IsValidTarget(Vector3 from, Vector3 to)
        {
            if (!CanReach(from, to))
                return false;
            Entity occupant = GetOccupant(to);
            if (occupant == default)
                return false;
            if (Has<CPreventItemTransfer>(occupant))
                return false;
            return true;
        }
    }
}
