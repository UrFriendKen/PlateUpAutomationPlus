using Kitchen;
using KitchenAutomationPlus.Customs;
using KitchenLib.Utils;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;

namespace KitchenAutomationPlus
{
    public struct CSingleStepAutomation : IComponentData
    {
        public int PreprocessedItemID;
    }

    public class AttachCSingleStepAutomation : StartOfDaySystem
    {
        private EntityQuery applianceQuery;

        private HashSet<int> _applicableApplianceIDs;

        protected override void Initialise()
        {
            base.Initialise();
            applianceQuery = GetEntityQuery(new QueryHelper()
                .All(typeof(CAppliance))
                .None(typeof(CSingleStepAutomation)));
            _applicableApplianceIDs = new HashSet<int>
            {
                GDOUtils.GetCustomGameDataObject<LazyMixer>().ID
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
                    Set(entity, new CSingleStepAutomation()
                    {
                        PreprocessedItemID = 0
                    });
                }
            }
            entities.Dispose();
        }
    }
}
