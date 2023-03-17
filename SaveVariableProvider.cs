using System.Collections.Generic;
using Kitchen;
using Unity.Collections;
using Unity.Entities;

namespace KitchenAutomationPlus
{
    public class SaveVariableProvider : GameSystemBase
    {
        private EntityQuery VariableProviders;

        private static List<CVariableProvider> Variables = new List<CVariableProvider>();

        private static List<CItemProvider> Providers = new List<CItemProvider>();

        private static List<CPosition> Positions = new List<CPosition>();

        protected override void Initialise()
        {
            base.Initialise();
            VariableProviders = GetEntityQuery(typeof(CVariableProvider), typeof(CItemProvider), typeof(CPosition));
        }

        protected override void OnUpdate()
        {
            if (!Has<SPracticeMode>())
            {
                return;
            }
            using NativeArray<CVariableProvider> variables = VariableProviders.ToComponentDataArray<CVariableProvider>(Allocator.Temp);
            using NativeArray<CItemProvider> providers = VariableProviders.ToComponentDataArray<CItemProvider>(Allocator.Temp);
            using NativeArray<CPosition> positions = VariableProviders.ToComponentDataArray<CPosition>(Allocator.Temp);
            Variables.Clear();
            Providers.Clear();
            Positions.Clear();
            foreach (CVariableProvider variable in variables)
            {
                Variables.Add(variable);
            }
            foreach (CItemProvider provider in providers)
            {
                Providers.Add(provider);
            }
            foreach (CPosition position in positions)
            {
                Positions.Add(position);
            }
        }

        public override void AfterLoading(SaveSystemType system_type)
        {
            base.AfterLoading(system_type);
            if (Variables == null || Positions == null)
            {
                return;
            }
            NativeArray<Entity> variableProviders = VariableProviders.ToEntityArray(Allocator.Temp);
            NativeArray<CVariableProvider> variables = VariableProviders.ToComponentDataArray<CVariableProvider>(Allocator.Temp);
            NativeArray<CItemProvider> providers = VariableProviders.ToComponentDataArray<CItemProvider>(Allocator.Temp);
            NativeArray<CPosition> positions = VariableProviders.ToComponentDataArray<CPosition>(Allocator.Temp);
            for (int i = 0; i < variableProviders.Length; i++)
            {
                for (int j = 0; j < Variables.Count; j++)
                {
                    if ((positions[i].Position - Positions[j].Position).Chebyshev() < 0.1f)
                    {
                        CVariableProvider newVariable = variables[i];
                        newVariable.Current = Variables[j].Current;
                        int provide = newVariable.Provide;
                        SetComponent(variableProviders[i], newVariable);
                        CItemProvider newProvider = providers[i];
                        newProvider.SetAsItem(provide);
                        SetComponent(variableProviders[i], newProvider);
                        break;
                    }
                }
            }
            Variables.Clear();
            Providers.Clear();
            Positions.Clear();
            variableProviders.Dispose();
            variables.Dispose();
            providers.Dispose();
            positions.Dispose();
        }
    }
}
