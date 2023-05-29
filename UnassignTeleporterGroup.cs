using Kitchen;
using KitchenMods;
using Unity.Entities;

namespace KitchenAutomationPlus
{
    public struct COrphanedTeleporter : IComponentData, IModComponent
    {
        public Entity PreviousTarget;
    }

    [UpdateBefore(typeof(ShowPingedApplianceInfo))]
    public class UnassignTeleporterGroup : InteractionSystem
    {
        CConveyTeleport Teleport;

        protected override bool AllowAnyMode => true;
        protected override InteractionType RequiredType => InteractionType.Notify;

        protected override bool IsPossible(ref InteractionData data)
        {
            if (Main.PrefManager.Get<int>(Main.TELEPORTER_ALLOW_UNASSIGN_ID) < 0)
            {
                return false;
            }
            if (Main.PrefManager.Get<int>(Main.TELEPORTER_ALLOW_UNASSIGN_ID) == 1 && !Has<SPracticeMode>())
            {
                return false;
            }
            if (Main.PrefManager.Get<int>(Main.TELEPORTER_ALLOW_UNASSIGN_ID) == 2 && !(Has<SPracticeMode>() || Has<SIsNightTime>()))
            {
                return false;
            }
            if (!Require(data.Target, out Teleport) || Teleport.Target == default)
            {
                return false;
            }
            return true;
        }

        protected override void Perform(ref InteractionData data)
        {
            if (!Has<COrphanedTeleporter>(data.Target))
            {
                if (Require(Teleport.Target, out CConveyTeleport targetTeleport) && targetTeleport.Target != default)
                {
                    Set(Teleport.Target, new COrphanedTeleporter()
                    {
                        PreviousTarget = data.Target
                    });
                }
            }
            else
            {
                EntityManager.RemoveComponent<COrphanedTeleporter>(data.Target);
            }
            Teleport.Target = default;
            Teleport.GroupID = 0;
            Set(data.Target, Teleport);
        }
    }
}
