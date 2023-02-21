using Kitchen;
using Unity.Collections;
using Unity.Entities;

namespace KitchenAutomationPlus.Systems
{
    public abstract class ComponentManagementSystem : GenericSystemBase
    {
        protected EntityQuery ManagedEntities;

        protected override void Initialise()
        {
            OnInitialise();
            ManagedEntities = AssignManagedEntities();
            OnPostInitialise();
        }

        protected override void OnUpdate()
        {
            OnPreUpdate();

            NativeArray<Entity> entities = ManagedEntities.ToEntityArray(Allocator.Temp);
            foreach (Entity entity in entities)
            {
                Perform(entity);
            }
            entities.Dispose();
            
            OnPostUpdate();
        }

        protected virtual void OnInitialise() { }

        protected virtual void OnPostInitialise() { }

        protected virtual void OnPreUpdate() { }

        protected virtual void OnPostUpdate() { }

        protected abstract EntityQuery AssignManagedEntities();

        protected abstract void Perform(Entity entity);
    }
}
