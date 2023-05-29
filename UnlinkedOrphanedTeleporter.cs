using Kitchen;
using KitchenMods;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;

namespace KitchenAutomationPlus
{
    public class UnlinkedOrphanedTeleporter : GenericSystemBase, IModSystem
    {
        EntityQuery Teleporters;

        protected override void Initialise()
        {
            base.Initialise();
            Teleporters = GetEntityQuery(new QueryHelper()
                .All(typeof(CConveyTeleport), typeof(COrphanedTeleporter)));
        }

        protected override void OnUpdate()
        {
            using NativeArray<Entity> entities = Teleporters.ToEntityArray(Allocator.Temp);
            using NativeArray<CConveyTeleport> teleports = Teleporters.ToComponentDataArray<CConveyTeleport>(Allocator.Temp);
            using NativeArray<COrphanedTeleporter> orphans = Teleporters.ToComponentDataArray<COrphanedTeleporter>(Allocator.Temp);

            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                CConveyTeleport teleport = teleports[i];
                COrphanedTeleporter orphan = orphans[i];

                if (orphan.PreviousTarget == default || !Require(orphan.PreviousTarget, out CConveyTeleport prevTeleport) || prevTeleport.Target != default)
                {
                    Unlink();
                }

                void Unlink()
                {
                    EntityManager.RemoveComponent<COrphanedTeleporter>(entity);
                    teleport.Target = default;
                    teleport.GroupID = 0;
                    Set(entity, teleport);
                }
            }
        }
    }
}
