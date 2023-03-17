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
    }
}
