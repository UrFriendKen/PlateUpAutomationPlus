using Kitchen;
using System.Linq;
using Unity.Collections;
using Unity.Entities;

namespace KitchenAutomationPlus.Systems.PseudoProcess
{

    internal class HandleDynamicChangeProvider : GameSystemBase
    {
        EntityQuery Query;

        protected override void Initialise()
        {
            base.Initialise();
            Query = GetEntityQuery(new QueryHelper()
                .All(typeof(CItemProvider), typeof(CTakesDuration), typeof(CDynamicChangeProvider))
                .None(typeof(CPreventUse)));
        }

        protected override void OnUpdate()
        {
            if (Has<SIsDayTime>())
            {
                NativeArray<Entity> entities = Query.ToEntityArray(Allocator.Temp);
                NativeArray<CItemProvider> providers = Query.ToComponentDataArray<CItemProvider>(Allocator.Temp);
                NativeArray<CDynamicChangeProvider> changes = Query.ToComponentDataArray<CDynamicChangeProvider>(Allocator.Temp);

                for (int i = 0; i < entities.Count(); i++)
                {
                    Entity entity = entities[i];
                    CItemProvider provider = providers[i];

                    if (!Require(entity, out CTakesDuration duration) || duration.Remaining > 0f || !duration.Active)
                    {
                        continue;
                    }

                    CDynamicChangeProvider change = changes[i];
                    if (change.TryGetReplacementItem(provider.ProvidedItem, out int replacementId))
                    {
                        provider.SetAsItem(replacementId);
                        Set(entity, provider);
                    }
                }

                entities.Dispose();
                providers.Dispose();
                changes.Dispose();
            }
        }
    }
}
