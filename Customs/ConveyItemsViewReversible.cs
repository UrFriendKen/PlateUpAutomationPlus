using Kitchen;
using KitchenData;
using MessagePack;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace KitchenAutomationPlus.Customs
{
    public class ConveyItemsViewReversible : UpdatableObjectView<ConveyItemsViewReversible.ViewData>
    {
        public class UpdateView : IncrementalViewSystemBase<ViewData>
        {
            EntityQuery _grabberQuery;

            protected override void Initialise()
            {
                base.Initialise();
                _grabberQuery = GetEntityQuery(new QueryHelper()
                    .All(typeof(CLinkedView), typeof(CConveyPushItemsReversible)));
            }

            protected override void OnUpdate()
            {
                using NativeArray<Entity> entities = _grabberQuery.ToEntityArray(Allocator.Temp);
                using NativeArray<CLinkedView> views = _grabberQuery.ToComponentDataArray<CLinkedView>(Allocator.Temp);
                using NativeArray<CConveyPushItemsReversible> grabs = _grabberQuery.ToComponentDataArray<CConveyPushItemsReversible>(Allocator.Temp);

                for (int i = 0; i < views.Length; i++)
                {
                    Entity entity = entities[i];
                    CLinkedView view = views[i];
                    CConveyPushItemsReversible grab = grabs[i];
                    SendUpdate(view.Identifier, new ViewData()
                    {
                        PushAmount = grab.Progress / grab.Delay,
                        State = grab.State,
                        SmartActive = grab.GrabSpecificType && grab.SpecificType != 0,
                        SmartFilter = grab.SpecificType,
                        SmartFilterComponents = grab.SpecificComponents,
                        GrabDirection = Require(entity, out CConveyPushRotatable rotate) ? rotate.Target : Orientation.Up,
                        Reversed = grab.Reversed
                    }, MessageType.SpecificViewUpdate);
                }
            }
        }

        [MessagePackObject(false)]
        public struct ViewData : ISpecificViewData, IViewData, IViewResponseData, IViewData.ICheckForChanges<ViewData>
        {
            [Key(1)]
            public float PushAmount;

            [Key(2)]
            public CConveyPushItems.ConveyState State;

            [Key(3)]
            public bool SmartActive;

            [Key(4)]
            public int SmartFilter;

            [Key(5)]
            public ItemList SmartFilterComponents;

            [Key(6)]
            public Orientation GrabDirection;

            [Key(7)]
            public bool Reversed;

            public bool IsChangedFrom(ViewData check)
            {
                return GrabDirection != check.GrabDirection || Mathf.Abs(PushAmount - check.PushAmount) > 0.001f || State != check.State || SmartActive != check.SmartActive || SmartFilter != check.SmartFilter || !SmartFilterComponents.IsEquivalent(check.SmartFilterComponents);
            }

            public IUpdatableObject GetRelevantSubview(IObjectView view)
            {
                return view.GetSubView<ConveyItemsViewReversible>();
            }
        }

        public GameObject PushObject;
        public GameObject SmartActive;
        public GameObject SmartInactive;
        public GameObject TypeContainer;
        public Animator Animator;
        public ViewData Data;

        private static readonly int Direction = Animator.StringToHash("Direction");

        protected override void UpdateData(ViewData view_data)
        {
            ViewData data = Data;
            Data = view_data;
            switch (view_data.State)
            {
                case CConveyPushItems.ConveyState.None:
                    PushObject.transform.localPosition = Vector3.zero;
                    break;
                case CConveyPushItems.ConveyState.Grab:
                    if (data.Reversed)
                        PushObject.transform.localPosition = Data.GrabDirection.ToOffset() * (1f - Data.PushAmount);
                    else
                        PushObject.transform.localPosition = Vector3.back * (1f - Data.PushAmount);
                    break;
                case CConveyPushItems.ConveyState.Push:
                    if (data.Reversed)
                        PushObject.transform.localPosition = Vector3.back * Data.PushAmount;
                    else
                        PushObject.transform.localPosition = Data.GrabDirection.ToOffset() * Data.PushAmount;
                    break;
            }

            bool flag = view_data.State == CConveyPushItems.ConveyState.Grab;
            if (SmartActive != null)
            {
                SmartActive.SetActive(flag && view_data.SmartActive);
            }
            if (SmartInactive != null)
            {
                SmartInactive.SetActive(!view_data.SmartActive);
            }
            if (Animator != null)
            {
                Animator.SetInteger(Direction, (int)view_data.GrabDirection);
            }
            if (TypeContainer != null && (data.SmartFilter != view_data.SmartFilter || !data.SmartFilterComponents.IsEquivalent(view_data.SmartFilterComponents)))
            {
                SetPrefab(GameData.Main.GetPrefab(view_data.SmartFilter), view_data.SmartFilter, view_data.SmartFilterComponents);
            }
        }

        public void SetPrefab(GameObject prefab, int item, ItemList components)
        {
            GameObject typeContainer = TypeContainer;
            GameObject gameObject = Instantiate(prefab, typeContainer.transform.parent, worldPositionStays: true);
            gameObject.transform.position = typeContainer.transform.position;
            gameObject.transform.rotation = typeContainer.transform.rotation;
            gameObject.transform.localScale = typeContainer.transform.localScale;
            gameObject.SetActive(typeContainer.activeSelf);
            if (gameObject.TryGetComponent<ItemGroupView>(out var component))
            {
                component.PerformUpdate(item, components);
            }
            TypeContainer = gameObject;
            Destroy(typeContainer);
        }
    }
}
