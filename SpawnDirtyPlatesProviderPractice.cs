using Kitchen;
using KitchenLib.References;
using System.Runtime.InteropServices;
using Unity.Entities;
using UnityEngine;

namespace KitchenAutomationPlus
{
    public class SpawnDirtyPlatesPractice : DaySystem
    {
        [StructLayout(LayoutKind.Sequential, Size = 1)]
        private struct SDirtyPlatesProvider : IComponentData
        {
        }

        protected override void OnUpdate()
        {
            if (!Has<SPracticeMode>())
                return;
            if (Has<SDirtyPlatesProvider>())
                return;
            if (!Main.PrefManager.Get<bool>(Main.SPAWN_DIRTY_PLATES_PRACTICE_ID))
                return;

            Vector3 frontDoor = GetFrontDoor();
            Entity entity = base.EntityManager.CreateEntity(typeof(CCreateAppliance), typeof(CPosition), typeof(SDirtyPlatesProvider));
            Set(entity, new CCreateAppliance()
            {
                ID = ApplianceReferences.DirtyPlateStackDEBUG
            });
            int direction = (frontDoor.x > 0f) ? -1 : 1;
            Set(entity, new CPosition(frontDoor + new Vector3(direction * 2, 0f, -1f)));
        }
    }
}
