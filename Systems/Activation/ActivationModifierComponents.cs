using Kitchen;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;

namespace KitchenAutomationPlus.Systems.Activation
{
    public struct CConditionalActivation : IComponentData, IModComponent
    {
        public bool IsAutomatic;
        public bool ActivateWhenFull;
        public bool ActivateWhenEmpty;
        public bool ActivateWhenHasItem;
        public bool AllowUseWhenEmpty;
        public bool OnlyWhenFull;
        public bool IsHolderFilter;
        public bool IsProviderFilter;
        public FixedListInt32 AllowedItems { get; private set; }

        public bool IsAllowed(CItem item)
        {
            return AllowedItems.Contains(item.GetHashCode());
        }

        public void AddAllowedItem(CItem item)
        {
            int hashCode = item.GetHashCode();
            if (!AllowedItems.Contains(hashCode))
            {
                AllowedItems.Add(hashCode);
            }
        }

        public void AddAllowedItem(IEnumerable<CItem> items)
        {
            foreach (CItem item in items)
            {
                int hashCode = item.GetHashCode();
                if (!AllowedItems.Contains(hashCode))
                {
                    AllowedItems.Add(hashCode);
                }
            }
        }

        public bool RemoveAllowedItem(CItem item)
        {
            int hashCode = item.GetHashCode();
            if (AllowedItems.Contains(hashCode))
            {
                AllowedItems.Remove(hashCode);
                return true;
            }
            return false;
        }

        public bool RemoveAllowedItem(IEnumerable<CItem> items)
        {
            bool success = true;
            foreach (CItem item in items)
            {
                int hashCode = item.GetHashCode();
                if (AllowedItems.Contains(hashCode))
                {
                    AllowedItems.Remove(hashCode);
                    continue;
                }
                success = false;
            }
            return success;
        }

        public bool RemoveAllowedItem(int index)
        {
            if (index > -1 && index < AllowedItems.Count())
            {
                AllowedItems.RemoveAt(index);
                return true;
            }
            return false;
        }
    }

    public struct CInhibitedActivation : IComponentData, IModComponent { }

    public struct CWasInhibited : IComponentData, IModComponent { }

    public struct CActivationNonpersistentCleanupMarker : IComponentData, IModComponent { }

    public struct CPerformAutomatedActivation : IComponentData, IModComponent { }
}
