using Kitchen;
using KitchenAutomationPlus.Preferences;

namespace KitchenAutomationPlus
{
    public class InteractRotatePushWithCondition : InteractRotatePush
    {
        protected override bool IsPossible(ref InteractionData data)
        {
            if (!Has<SPracticeMode>() && Main.PrefManager.Get<int>(Main.GRABBER_ALLOW_ROTATE_DURING_DAY_ID) > 0)
            {
                return false;
            }
            return base.IsPossible(ref data);
        }
    }
}
