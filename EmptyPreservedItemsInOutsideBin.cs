using Kitchen;
using Unity.Entities;

namespace KitchenAutomationPlus
{
    internal class EmptyPreservedItemsInOutsideBin : ApplianceInteractionSystem
    {
        protected override InteractionType RequiredType => InteractionType.Grab;

        private CItemHolder Holder;

        protected override bool IsPossible(ref InteractionData data)
        {
            if (!Require(data.Interactor, out Holder) || Holder.HeldItem == default)
            {
                return false;
            }
            if (!Has<CPreservesContentsOvernight>(Holder.HeldItem))
            {
                return false;
            }
            if (!Has<CApplianceExternalBin>(data.Target))
            {
                return false;
            }
            return true;
        }

        protected override void Perform(ref InteractionData data)
        {
            Entity appliance = Holder.HeldItem;
            if (Require(appliance, out CItemHolder holder) && holder.HeldItem != default)
            {
                EntityManager.DestroyEntity(holder.HeldItem);
            }
            if (Require(appliance, out CItemProvider provider))
            {
                provider.Available = 0;
                Set(appliance, provider);
            }
        }
    }
}
