using Kitchen;
using Unity.Entities;

namespace KitchenAutomationPlus
{
    [UpdateBefore(typeof(ShowPingedApplianceInfo))]
    public class SwitchVariableProviderDuringNight : ApplianceInteractionSystem
    {
        protected override InteractionType RequiredType => InteractionType.Notify;

        private CVariableProvider VariableProvider;

        private CItemProvider Provider;

        protected override bool IsPossible(ref InteractionData data)
        {
            if (!Require(data.Target, out VariableProvider))
                return false;

            if (!Require(data.Target, out Provider))
                return false;

            if (!Main.PrefManager.Get<bool>(Main.VARIABLE_PROVIDER_ALLOW_SWITCH_DURING_NIGHT_ID))
                return false;

            return true;
        }

        protected override void Perform(ref InteractionData data)
        {
            VariableProvider.Current = (VariableProvider.Current + 1) % 3;
            int provide = VariableProvider.Provide;
            SetComponent(data.Target, VariableProvider);
            Provider.SetAsItem(provide);
            SetComponent(data.Target, Provider);
        }
    }
}
