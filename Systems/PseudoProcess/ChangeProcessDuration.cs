using Kitchen;
using System.Linq;
using Unity.Collections;
using Unity.Entities;

namespace KitchenAutomationPlus.Systems.PseudoProcess
{

    internal class ChangeProcessDuration : GameSystemBase
    {
        EntityQuery Query;

        protected override void Initialise()
        {
            base.Initialise();
            Query = GetEntityQuery(new QueryHelper()
                .All(typeof(CItemProvider), typeof(CTakesDuration), typeof(CPseudoProcessDuration)));
        }

        protected override void OnUpdate()
        {
            NativeArray<Entity> entities = Query.ToEntityArray(Allocator.Temp);
            NativeArray<CItemProvider> providers = Query.ToComponentDataArray<CItemProvider>(Allocator.Temp);
            NativeArray<CTakesDuration> durations = Query.ToComponentDataArray<CTakesDuration>(Allocator.Temp);
            NativeArray<CPseudoProcessDuration> customDurations = Query.ToComponentDataArray<CPseudoProcessDuration>(Allocator.Temp);

            for (int i = 0; i < entities.Count(); i++)
            {
                Entity entity = entities[i];
                CItemProvider provider = providers[i];
                CTakesDuration duration = durations[i];
                CPseudoProcessDuration customDuration = customDurations[i];
                if (Has<SIsDayTime>())
                {
                    if (provider.Available != 0 && customDuration.TryGetDuration(provider.ProvidedItem, out float newTotal))
                    {
                        duration.Total = newTotal;
                    }
                    else
                    {
                        duration.Total = customDuration.BaseDuration;
                    }
                }
                else if (Has<SIsNightFirstUpdate>())
                {
                    duration.Total = customDuration.BaseDuration;
                }
                Set(entity, duration);
            }

            entities.Dispose();
            providers.Dispose();
            durations.Dispose();
            customDurations.Dispose();
        }
    }
}
