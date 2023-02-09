using Kitchen;
using KitchenLib.References;
using Unity.Collections;
using Unity.Entities;

namespace KitchenAutomationPlus
{
    internal class ReplaceRefilledBroth : GameSystemBase
    {
        EntityQuery entityQuery;

        protected override void Initialise()
        {
            base.Initialise();
            entityQuery = GetEntityQuery(new QueryHelper()
                .All(typeof(CItem), typeof(CHeldBy))
                .Any()
                .None());
        }

        protected override void OnUpdate()
        {
            if (Main.PrefManager.Get<int>(Main.REFILLED_BROTH_CHANGE_ID) == 1)
            {
                NativeArray<Entity> entities = entityQuery.ToEntityArray(Allocator.Temp);
                foreach (Entity entity in entities)
                {
                    if (Require(entity, out CItem item) && item.ID == ItemReferences.SoupRefilled)
                    {
                        Main.LogInfo("Changed SoupRefilled to BrothRawOnion.");
                        item.ID = ItemReferences.BrothRawOnion;
                        // Do it properly as item group referencing BrothRawOnion
                        item.Items = new KitchenData.ItemList();
                        item.Items.Add(ItemReferences.Water);
                        item.Items.Add(ItemReferences.Onion);
                        item.Items.Add(ItemReferences.Pot);
                        EntityManager.SetComponentData(entity, item);
                    }
                }
                entities.Dispose();
            }
        }
    }
}
