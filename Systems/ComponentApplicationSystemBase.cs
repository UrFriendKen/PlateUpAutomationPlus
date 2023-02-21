using Kitchen;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;

namespace KitchenAutomationPlus.Systems
{
    public abstract class ComponentApplicationSystemBase : ComponentManagementSystem
    {
        protected sealed override void Initialise()
        {
            base.Initialise();
            RequireSingletonForUpdate<SIsDayTime>();
        }

        protected abstract List<IComponentData> Components { get; }

        protected sealed override void Perform(Entity entity)
        {
            if (IsApplyComponents(entity))
            {
                foreach (var component in Components)
                {
                    EntityManager.SetComponentData(entity, component);
                }
                return;
            }

            if (IsRemoveComponents(entity))
            {
                foreach (var component in Components)
                {
                    EntityManager.RemoveComponent(entity, component.GetType());
                }
            }
        }

        protected virtual bool IsApplyComponents(Entity entity)
        {
            return false;
        }

        protected virtual bool IsRemoveComponents(Entity entity)
        {
            return false;
        }
    }
}
