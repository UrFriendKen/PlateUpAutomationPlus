using KitchenData;
using KitchenMods;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;

namespace KitchenAutomationPlus.Systems.PseudoProcess
{
    public struct CPseudoProcessDuration : IComponentData, IApplianceProperty, IModComponent
    {
        public float BaseDuration;
        private FixedListInt128 Items;
        private FixedListFloat128 CustomDurations;

        public CPseudoProcessDuration(float baseDuration, int[] itemIds, float[] durations)
        {
            BaseDuration = baseDuration;

            if (itemIds.Length != durations.Length)
            {
                throw new ArgumentException("Number of elements in idsBefore and idsAfter must be equal!");
            }

            Items = new FixedListInt128();
            CustomDurations = new FixedListFloat128();
            for (int i = 0; i < itemIds.Length; i++)
            {
                Items.Add(itemIds[i]);
                CustomDurations.Add(durations[i]);
            }
        }

        public bool TryGetDuration(int itemId, out float duration)
        {
            duration = BaseDuration;
            if (Items.Contains(itemId))
            {
                for (int i = 0; i < Items.Length; i++)
                {
                    if (Items[i] == itemId)
                    {
                        duration = CustomDurations[i];
                        return true;
                    }
                }
            }
            return false;
        }

        public Dictionary<int, float> GetDictionary()
        {
            Dictionary<int, float> dict = new Dictionary<int, float>();
            for (int i = 0; i < Items.Length; i++)
            {
                dict.Add(Items[i], CustomDurations[i]);
            }
            return dict;
        }
    }
}
