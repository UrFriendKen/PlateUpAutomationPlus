using KitchenData;
using KitchenMods;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;

namespace KitchenAutomationPlus.Systems.PseudoProcess
{
    public struct CDynamicChangeProvider : IComponentData , IApplianceProperty, IModComponent
    {
        private FixedListInt128 Before;
        private FixedListInt128 After;

        public Dictionary<int, int> this[int index]
        {
            get
            {
                Dictionary<int, int> dict = new Dictionary<int, int>();
                for (int i = 0; i < Before.Length; i++)
                {
                    dict.Add(Before[i], After[i]);
                }
                return dict;
            }
        }

        public CDynamicChangeProvider(int[] idsBefore, int[] idsAfter)
        {
            if (idsBefore.Length != idsAfter.Length)
            {
                throw new ArgumentException("Number of elements in idsBefore and idsAfter must be equal!");
            }

            Before = new FixedListInt128();
            After = new FixedListInt128();
            for (int i = 0; i < idsBefore.Length; i++)
            {
                Before.Add(idsBefore[i]);
                After.Add(idsAfter[i]);
            }
        }

        public bool Add(int idBefore, int idAfter)
        {
            bool found = true;
            if (Main.Find<Item>(idBefore) == null)
            {
                Main.LogWarning($"GDO ({idBefore}) not found!");
                found = false;
            }
            if (Main.Find<Item>(idAfter) == null)
            {
                Main.LogWarning($"GDO ({idAfter}) not found!");
                found = false;
            }

            if (!found)
            {
                Main.LogWarning("One or more GDOs not found! Skipping.");
                return false;
            }
            Before.Add(idBefore);
            After.Add(idAfter);
            Main.LogInfo("Successfully added");
            return true;
        }

        public bool TryGetReplacementItem(int idBefore, out int replacementId)
        {
            replacementId = 0;
            if (Before.Contains(idBefore))
            {
                for (int i = 0; i < Before.Length; i++)
                {
                    if (Before[i] == idBefore)
                    {
                        replacementId = After[i];
                        return true;
                    }
                }
            }
            return false;
        }

        public Dictionary<int, int> GetDictionary()
        {
            Dictionary<int, int> dict = new Dictionary<int, int>();
            for (int i = 0; i < Before.Length; i++)
            {
                dict.Add(Before[i], After[i]);
            }
            return dict;
        }
    }
}
