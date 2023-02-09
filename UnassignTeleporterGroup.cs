using Kitchen;
using KitchenAutomationPlus.Preferences;
using Unity.Entities;

namespace KitchenAutomationPlus
{
    [UpdateBefore(typeof(ShowPingedApplianceInfo))]
    public class UnassignTeleporterGroup : InteractionSystem
    {
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
            if (!Require(data.Target, out CConveyTeleport teleport) || teleport.Target == default(Entity))
            {
                return false;
            }
            return true;
        }

        protected override void Perform(ref InteractionData data)
        {
            if (Require(data.Target, out CConveyTeleport teleport))
            {
                teleport.Target = default(Entity);
                teleport.GroupID = 0;
                Set(data.Target, teleport);
            }
        }
    }
}
