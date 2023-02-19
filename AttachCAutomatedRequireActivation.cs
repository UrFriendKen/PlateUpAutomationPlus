using Kitchen;
using KitchenAutomationPlus.Customs;
using KitchenLib.References;
using KitchenLib.Utils;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;

namespace KitchenAutomationPlus
{
    internal struct CAutomatedRequireActivation : IComponentData
    {
        public bool IsSingleStep;
        public bool IsRequireItem;
        public bool Performed;
    }

    public class AttachCAutomatedRequireActivation : StartOfDaySystem
    {
        private EntityQuery applianceQuery;

        private HashSet<int> _applicableApplianceIDs;

        protected override void Initialise()
        {
            base.Initialise();
            applianceQuery = GetEntityQuery(new QueryHelper()
                .All(typeof(CAppliance))
                .None(typeof(CAutomatedRequireActivation)));
            _applicableApplianceIDs = new HashSet<int>
            {
                GDOUtils.GetExistingGDO(ApplianceReferences.Microwave).ID
            };
        }

        protected override void OnUpdate()
        {
            NativeArray<Entity> entities = applianceQuery.ToEntityArray(Allocator.Temp);
            foreach (Entity entity in entities)
            {
                if (!Require(entity, out CAppliance appliance))
                {
                    continue;
                }
                if (_applicableApplianceIDs.Contains(appliance.ID))
                {
                    Set(entity, new CAutomatedRequireActivation()
                    {
                        IsSingleStep = true,
                        IsRequireItem = true,
                        Performed = false
                    });
                }
            }
            entities.Dispose();
        }
    }
}
