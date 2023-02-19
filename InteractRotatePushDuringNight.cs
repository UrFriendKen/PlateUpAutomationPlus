using Kitchen;
using KitchenAutomationPlus.Preferences;
using Unity.Entities;

namespace KitchenAutomationPlus
{
    [UpdateInGroup(typeof(InteractionGroup))]
    [UpdateBefore(typeof(ShowPingedApplianceInfo))]
    public class InteractRotatePushDuringNight : InteractRotatePush
    {
        protected override InteractionMode RequiredMode => InteractionMode.Appliances;

        protected override InteractionType RequiredType => InteractionType.Notify;

        protected override bool IsPossible(ref InteractionData data)
        {
            if (!Has<SIsNightTime>() || (-1 < Main.PrefManager.Get<int>(Main.GRABBER_ALLOW_ROTATE_DURING_DAY_ID) && Main.PrefManager.Get<int>(Main.GRABBER_ALLOW_ROTATE_DURING_DAY_ID) < 2))
            {
                return false;
            }
            return base.IsPossible(ref data);
        }

        protected override void Perform(ref InteractionData data)
        {
            base.Perform(ref data);
        }
    }
}
