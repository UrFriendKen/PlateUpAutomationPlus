using Kitchen;
using KitchenData;
using KitchenMods;
using Unity.Collections;
using Unity.Entities;

namespace KitchenAutomationPlus
{

    [UpdateInGroup(typeof(ActivateEffectsGroup))]
    public class ActivatePreferenceConditional : GameSystemBase
    {
        public enum EffectCondition
        {
            Never,
            Always,
            WhileBeingUsed,
            AtDay,
            AtNight
        }
        public struct CEffectPreferenceConditional : IEffectCondition, IComponentData, IModComponent
        {
            public FixedString128 PreferenceID;
            public EffectCondition ConditionWhenEnabled;
            public EffectCondition ConditionWhenDisabled;
        }

        EntityQuery AppliesEffects;
        protected override void Initialise()
        {
            base.Initialise();
            AppliesEffects = GetEntityQuery(new QueryHelper()
                .All(typeof(CAppliesEffect), typeof(CEffectPreferenceConditional)));
        }

        protected override void OnUpdate()
        {
            using NativeArray<Entity> entities = AppliesEffects.ToEntityArray(Allocator.Temp);
            using NativeArray<CEffectPreferenceConditional> preferences = AppliesEffects.ToComponentDataArray<CEffectPreferenceConditional>(Allocator.Temp);
            using NativeArray<CAppliesEffect> appliesEffects = AppliesEffects.ToComponentDataArray<CAppliesEffect>(Allocator.Temp);

            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                CEffectPreferenceConditional preference = preferences[i];
                CAppliesEffect effect = appliesEffects[i];

                bool preferenceEnabled = Main.PrefManager.Get<bool>(preference.PreferenceID.ConvertToString());
                EffectCondition activeCondition = preferenceEnabled? preference.ConditionWhenEnabled : preference.ConditionWhenDisabled;
                effect.IsActive = GetState(activeCondition, in entity);
                Set(entity, effect);
            }
        }

        private bool GetState(EffectCondition condition, in Entity entity)
        {
            float timeOfDay = GetOrDefault<STime>().TimeOfDay;
            bool is_night = timeOfDay > 0.66f;
            switch (condition)
            {
                case EffectCondition.Never:
                    return false;
                case EffectCondition.Always:
                    return true;
                case EffectCondition.WhileBeingUsed:
                    if (RequireBuffer(entity, out DynamicBuffer<CBeingActedOnBy> actors))
                    {
                        foreach (CBeingActedOnBy actor in actors)
                        {
                            if (!actor.IsTransferOnly)
                            {
                                return true;
                            }
                        }
                    }
                    return false;
                case EffectCondition.AtDay:
                    return !is_night;
                case EffectCondition.AtNight:
                    return is_night;
                default:
                    break;
            }
            return false;
        }
    }
}
