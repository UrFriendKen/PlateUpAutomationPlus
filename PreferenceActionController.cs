using Kitchen;
using Kitchen.Layouts;
using KitchenData;
using KitchenMods;
using Unity.Collections;
using Unity.Entities;

namespace KitchenAutomationPlus
{
    public struct SMakeSpaceOutside : IComponentData, IModComponent { }

    public class PreferenceActionController : GameSystemBase, IModSystem
    {
        EntityQuery AppliancesOutsideQuery;
        EntityQuery BlueprintsQuery;
        EntityQuery SingletonEntitiesQuery;

        static PreferenceActionController _instance;

        protected override void Initialise()
        {
            base.Initialise();
            _instance = this;
            AppliancesOutsideQuery = GetEntityQuery(new QueryHelper()
                .All(typeof(CDestroyApplianceAtDay))
                .None(typeof(CTriggerPracticeMode), typeof(CRerollShopAfterDuration), typeof(CApplianceBlueprint), typeof(CLetterBlueprint), typeof(CLetterAppliance), typeof(CLetterIngredient)));

            BlueprintsQuery = GetEntityQuery(new QueryHelper()
                .All(typeof(CApplianceBlueprint), typeof(CPosition)));

            SingletonEntitiesQuery = GetEntityQuery(new QueryHelper()
                .Any(typeof(SMakeSpaceOutside)));
        }

        protected override void OnUpdate()
        {
            if (GameInfo.CurrentScene == SceneType.Kitchen && Has<SIsNightTime>())
            {
                if (Has<SMakeSpaceOutside>())
                {
                    EntityManager.DestroyEntity(AppliancesOutsideQuery);

                    NativeArray<Entity> blueprintEntities = BlueprintsQuery.ToEntityArray(Allocator.Temp);
                    NativeArray<CPosition> blueprintPositions = BlueprintsQuery.ToComponentDataArray<CPosition>(Allocator.Temp);
                    CPosition newBlueprintPosition = GetFrontDoor(true);
                    for (int i = 0; i < blueprintEntities.Length; i++)
                    {
                        Entity blueprintEntity = blueprintEntities[i];
                        CPosition oldBlueprintPosition = blueprintPositions[i];

                        if (!LayoutHelpers.IsOutsidePlayable(GetTile(oldBlueprintPosition).Type))
                        {
                            continue;
                        }
                        Set(blueprintEntity, newBlueprintPosition);
                        SetOccupant(oldBlueprintPosition, default(Entity), OccupancyLayer.Default);
                        SetOccupant(newBlueprintPosition, blueprintEntity, OccupancyLayer.Default);
                    }
                }
            }
            EntityManager.DestroyEntity(SingletonEntitiesQuery);
        }

        public static void MakeSpaceOutside()
        {
            _instance?.Set<SMakeSpaceOutside>();
        }
    }
}
