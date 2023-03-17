using Kitchen;
using Unity.Entities;

namespace KitchenAutomationPlus
{
    [UpdateInGroup(typeof(InteractionGroup))]
    [UpdateBefore(typeof(ShowPingedApplianceInfo))]
    public class SwitchVariableProviderDuringNight : SwitchVariableProvider
    {
        protected override InteractionMode RequiredMode => InteractionMode.Appliances;

        protected override InteractionType RequiredType => InteractionType.Notify;

        protected override bool IsPossible(ref InteractionData data)
        {
            if (!Main.PrefManager.Get<bool>(Main.VARIABLE_PROVIDER_ALLOW_SWITCH_DURING_NIGHT_ID))
            {
                return false;
            }
            return base.IsPossible(ref data);
        }
    }
}
