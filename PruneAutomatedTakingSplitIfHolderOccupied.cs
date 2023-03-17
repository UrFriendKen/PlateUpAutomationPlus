using Kitchen;
using KitchenLib.References;
using Unity.Collections;
using Unity.Entities;

namespace KitchenAutomationPlus
{
    [UpdateInGroup(typeof(ItemTransferEarlyPrune))]
    internal class PruneAutomatedTakingSplitIfHolderOccupied : GenericSystemBase
    {
        private EntityQuery TransferProposals;

        protected override void Initialise()
        {
            TransferProposals = GetEntityQuery(
                typeof(CItemTransferProposal)
            );
        }

        protected override void OnUpdate()
        {
            using var entities = TransferProposals.ToEntityArray(Allocator.Temp);
            using var itemTransferProposals = TransferProposals.ToComponentDataArray<CItemTransferProposal>(Allocator.Temp);

            for (int i = 0; i < entities.Length; ++i)
            {
                var entity = entities[i];
                var proposal = itemTransferProposals[i];

                if (proposal.Status == ItemTransferStatus.Pruned)
                {
                    continue;
                }

                if (Require<CAutomatedInteractor>(proposal.Destination, out CAutomatedInteractor auto) && auto.IsHeld && Require(proposal.Destination, out CAppliance appliance) && appliance.ID == ApplianceReferences.Portioner && 
                    Require(proposal.Destination, out CItemHolder holder) && holder.HeldItem != default&& proposal.Flags.HasFlag(TransferFlags.Split) && Require(proposal.Destination, out CPosition position) && GetOccupant(position.ForwardPosition) == proposal.Source)
                {
                    if (Main.PrefManager.Get<bool>(Main.PORTIONER_DISALLOW_AUTO_SPLIT_MERGE))
                    {
                        proposal.Status = ItemTransferStatus.Pruned;
                    }
                }

                if (proposal.Status == ItemTransferStatus.Pruned)
                {
                    proposal.PrunedBy = this;
                }

                SetComponent(entity, proposal);
            }
        }
    }
}
