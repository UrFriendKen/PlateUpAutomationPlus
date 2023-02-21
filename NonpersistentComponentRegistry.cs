using KitchenAutomationPlus.Systems.Activation;
using System;
using System.Collections.Generic;
using Unity.Entities;

namespace KitchenAutomationPlus
{
    public static class NonpersistentComponentRegistry
    {
        internal static Dictionary<int, CConditionalActivation> ConditionalActivationDirectory = new Dictionary<int, CConditionalActivation>();

        public static bool AddNonpersistentComponent<T>(int gdo_id, T component) where T : IComponentData
        {
            Type type = component.GetType();

            if (type == typeof(CConditionalActivation))
            {
                if (!ConditionalActivationDirectory.ContainsKey(gdo_id))
                {
                    ConditionalActivationDirectory.Add(gdo_id, (CConditionalActivation)Convert.ChangeType(component, typeof(CConditionalActivation)));
                    return true;
                }
                return false;
            }
            return false;
        }
    }
}
