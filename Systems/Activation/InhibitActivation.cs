using Kitchen;
using System.Collections.Generic;
using Unity.Entities;

namespace KitchenAutomationPlus.Systems.Activation
{
    internal class CleanupWasInhibitedMarker : ComponentEarlyApplicationSystem
    {
        protected override List<IComponentData> Components => new List<IComponentData>
        {
            new CWasInhibited()
        };

        protected override EntityQuery AssignManagedEntities()
        {
            return GetEntityQuery(typeof(CWasInhibited));
        }

        protected override bool IsRemoveComponents(Entity entity)
        {
            return true;
        }
    }

    internal abstract class InhibitActivation : ComponentApplicationSystem
    {
        protected override List<IComponentData> Components => new List<IComponentData>
        {
            new CInhibitedActivation()
        };
    }

    internal class ProviderInhibitActivation : InhibitActivation
    {
        protected override EntityQuery AssignManagedEntities()
        {
            return GetEntityQuery(new QueryHelper()
                .All(typeof(CItemProvider), typeof(CConditionalActivation))
                .None(typeof(CWasInhibited)));
        }

        protected override bool IsApplyComponents(Entity entity)
        {
            if (!Require(entity, out CConditionalActivation condition) || !condition.IsProviderFilter)
            {
                return true;
            }
            if (!Require(entity, out CItemProvider provider))
            {
                return true;
            }
            if (provider.Available == 0 && !condition.AllowUseWhenEmpty)
            {
                return true;
            }
            if (provider.Available < provider.Maximum && condition.OnlyWhenFull)
            {
                return true;
            }
            if (!condition.IsAllowed(provider.ProvidedItem))
            {
                return true;
            }
            return false;
        }
    }

    internal class HolderInhibitActivation : InhibitActivation
    {
        protected override EntityQuery AssignManagedEntities()
        {
            return GetEntityQuery(new QueryHelper()
                .All(typeof(CItemHolder), typeof(CConditionalActivation))
                .None(typeof(CWasInhibited)));
        }

        protected override bool IsApplyComponents(Entity entity)
        {
            if (!Require(entity, out CConditionalActivation condition) || !condition.IsHolderFilter)
            {
                return true;
            }
            if (!Require(entity, out CItemHolder holder))
            {
                return true;
            }
            if (holder.HeldItem == default && !condition.AllowUseWhenEmpty)
            {
                return true;
            }
            if (Require(holder.HeldItem, out CItem item) && !condition.IsAllowed(item))
            {
                return true;
            }
            return false;
        }
    }
    internal class RemoveInhibitActivation: ComponentLateApplicationSystem
    {
        protected override List<IComponentData> Components => new List<IComponentData>
        {
            new CInhibitedActivation()
        };

        protected override EntityQuery AssignManagedEntities()
        {
            return GetEntityQuery(new QueryHelper()
                .All(typeof(CInhibitedActivation))
                .None(typeof(CWasInhibited)));
        }

        protected override bool IsRemoveComponents(Entity entity)
        {
            return true;
        }
    }

    internal class CheckForAutomatedActivation : ComponentLateApplicationSystem
    {
        protected override List<IComponentData> Components => new List<IComponentData>
        {
            new CPerformAutomatedActivation()
        };

        protected override EntityQuery AssignManagedEntities()
        {
            return GetEntityQuery(new QueryHelper()
                .All(typeof(CConditionalActivation))
                .Any(typeof(CItemProvider), typeof(CItemHolder))
                .None(typeof(CInhibitedActivation)));
        }

        protected override bool IsApplyComponents(Entity entity)
        {
            if (!Require(entity, out CConditionalActivation condition) || !condition.IsAutomatic)
            {
                return false;
            }
            if (condition.IsHolderFilter)
            {
                if (!Require(entity, out CItemHolder holder))
                {
                    return false;
                }

                if (condition.ActivateWhenEmpty && holder.HeldItem == default)
                {
                    return true;
                }
                if ((condition.ActivateWhenFull || condition.ActivateWhenHasItem) && holder.HeldItem != default)
                {
                    return true;
                }
            }
            if (condition.IsProviderFilter)
            {
                if (!Require(entity, out CItemProvider provider))
                {
                    return false;
                }

                if (condition.ActivateWhenEmpty && provider.Available == 0)
                {
                    return true;
                }
                if (condition.ActivateWhenHasItem && provider.Available > 0)
                {
                    return true;
                }
                if (condition.ActivateWhenFull && provider.Available == provider.Maximum)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
