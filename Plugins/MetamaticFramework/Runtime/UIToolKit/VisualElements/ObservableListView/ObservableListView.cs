using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

namespace Metamatic.UIToolKit
{
    public class ObservableListView<T>: AsyncVisualElement, IDisposable
    {
        #region Static
        private static UniTaskVoid DefaultItemRenderer(T item, VisualElement container, CancellationSignal signal)
        {
            container.Add(new Label(item.ToString()));
            return new UniTaskVoid();
        }
        #endregion
        public ObservableList<T> source { get; private set; }
        private readonly ObservableList<T> m_SelectedItems = new ObservableList<T>();
        
        public IReadOnlyCollection<T> SelectedItems => m_SelectedItems.items;
        #region Config
        private SelectionType m_SelectionType;
        private bool m_InteractiveSelection;
        #endregion
        private readonly Dictionary<T, VisualElement> m_MapToElement = new Dictionary<T, VisualElement>();
        public Func<T, VisualElement, CancellationSignal, UniTaskVoid> itemRenderer { get; set; } = DefaultItemRenderer;
        public ObservableListView(ObservableList<T> observableList, SelectionType selectionType = SelectionType.None, Func<T, VisualElement, CancellationSignal, UniTaskVoid> itemRenderer = null, bool interactiveSelection = true)
        {
            Init(observableList, selectionType, itemRenderer, interactiveSelection);
        }
        
        public ObservableListView(SelectionType selectionType = SelectionType.None, Func<T, VisualElement, CancellationSignal, UniTaskVoid> itemRenderer = null, bool interactiveSelection = true)
        {
            var observableList = new ObservableList<T>();
            Init(observableList, selectionType, itemRenderer, interactiveSelection);
        }
        
        ~ObservableListView()
        {
            Console.Exception("ObservableListView", "Was not disposed. You should dispose it manually.");
        }
        
        private void Init(ObservableList<T> observableList, SelectionType selectionType = SelectionType.None, Func<T, VisualElement, CancellationSignal, UniTaskVoid> itemRenderer = null, bool interactiveSelection = true)
        {
            styleSheets.Add(UIToolKitCore.styleSheet);
            source = observableList;
            m_SelectionType = selectionType;
            m_InteractiveSelection = interactiveSelection;
            if (itemRenderer != null)
            {
                this.itemRenderer = itemRenderer;
            }
            AddToClassList("MetamaticUIKit_ItemsList");
        }
        
        public void Select(T item)
        {
            if (m_SelectedItems.Contains(item))
            {
                return;
            }
            if (m_SelectionType == SelectionType.Single)
            {
                m_SelectedItems.Clear();
            }

            if (!source.Contains(item))
            {
                return;
            }
            m_SelectedItems.Add(item);
        }
        
        /// <summary>
        /// Watch for selected items lifecycle
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public DisposableAction WatchSelectedLifecycle(Func<T, int, CancellationSignal, UniTaskVoid> action)
        {
            return m_SelectedItems.WatchEachLifecycle(action);
        }

        public override async UniTask OnAttachedAsync(CancellationSignal detachSignal)
        {
            using var watchSelected = m_SelectedItems.WatchEachLifecycle(OnSelectedChange);
            using var watcher = source.WatchEachLifecycle(OnItemLifecycle);
            await detachSignal;
        }
        
        private async UniTaskVoid OnSelectedChange(T item, int index, CancellationSignal signal)
        {
            if (m_MapToElement.TryGetValue(item, out var element))
            {
                element.AddToClassList("MetamaticUIKit_Selected");
            }

            await signal;
            
            if (m_MapToElement.TryGetValue(item, out var element2))
            {
                element2.RemoveFromClassList("MetamaticUIKit_Selected");
            }
        }

        private async UniTaskVoid OnItemLifecycle(T item, int index, CancellationSignal signal)
        {
            var itemElement = new VisualElement();
            itemElement.AddToClassList("MetamaticUIKit_Item");
            Add(itemElement);
            m_MapToElement.Add(item, itemElement);

            if (m_SelectionType != SelectionType.None)
            {
                if (m_InteractiveSelection)
                {
                    // Listen for click
                    itemElement.RegisterCallback<ClickEvent>(evt =>
                    {
                        if (m_SelectedItems.Contains(item))
                        {
                            m_SelectedItems.Remove(item);
                            return;
                        }

                        if (m_SelectionType == SelectionType.Single)
                        {
                            m_SelectedItems.Clear();
                        }

                        m_SelectedItems.Add(item);
                    });
                }

                // Add indicator for item
                var indicator = new VisualElement
                {
                    name = "Indicator"
                };
                indicator.AddToClassList("MetamaticUIKit_ItemIndicator");
                itemElement.Add(indicator);
            }
            
            // Add label
            var itemElementValueContainer = new VisualElement();
            itemElementValueContainer.AddToClassList("MetamaticUIKit_ItemContent");
            itemElement.Add(itemElementValueContainer);
            itemRenderer(item, itemElementValueContainer, signal).Forget();
            
            await signal;
            m_MapToElement.Remove(item);
            Remove(itemElement);
        }

        public void Dispose()
        {
            source.Dispose();
            m_SelectedItems.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}