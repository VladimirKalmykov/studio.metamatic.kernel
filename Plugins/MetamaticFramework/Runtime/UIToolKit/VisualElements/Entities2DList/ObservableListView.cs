using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

namespace Metamatic.UIToolKit
{
    public class ObservableListView<T>: AsyncVisualElement
    {
        public enum SelectionType
        {
            None,
            Single,
            Multiple
        }
        #region Static
        private static UniTaskVoid DefaultItemRenderer(T item, VisualElement container, CancellationSignal signal)
        {
            container.Add(new Label(item.ToString()));
            return new UniTaskVoid();
        }
        #endregion
        private ObservableList<T> m_ObservableList;
        private readonly ObservableList<T> m_SelectedItems = new ObservableList<T>();
        public IReadOnlyCollection<T> SelectedItems => m_SelectedItems.items;
        private SelectionType m_SelectionType;
        private readonly Dictionary<T, VisualElement> m_MapToElement = new Dictionary<T, VisualElement>();
        public Func<T, VisualElement, CancellationSignal, UniTaskVoid> itemRenderer { get; set; } = DefaultItemRenderer;
        public ObservableListView(ObservableList<T> observableList, SelectionType selectionType = SelectionType.None, Func<T, VisualElement, CancellationSignal, UniTaskVoid> itemRenderer = null)
        {
            styleSheets.Add(UIToolKitCore.styleSheet);
            m_ObservableList = observableList;
            m_SelectionType = selectionType;
            if (itemRenderer != null)
            {
                this.itemRenderer = itemRenderer;
            }
            AddToClassList("MetamaticUIKit_ItemsList");
        }
        
        /// <summary>
        /// Watch for selected items lifecycle
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public DisposableAction WatchSelectedLifecycle(Func<T, CancellationSignal, UniTaskVoid> action)
        {
            return m_SelectedItems.WatchEachLifecycle(action);
        }

        public override async UniTask OnAttachedAsync(CancellationSignal detachSignal)
        {
            using var watchSelected = m_SelectedItems.WatchEachLifecycle(OnSelectedChange);
            using var watcher = m_ObservableList.WatchEachLifecycle(OnItemLifecycle);
            await detachSignal;
        }
        
        private async UniTaskVoid OnSelectedChange(T item, CancellationSignal signal)
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

        private async UniTaskVoid OnItemLifecycle(T item, CancellationSignal signal)
        {
            var itemElement = new VisualElement();
            itemElement.AddToClassList("MetamaticUIKit_Item");
            Add(itemElement);
            m_MapToElement.Add(item, itemElement);

            if (m_SelectionType != SelectionType.None)
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
    }
}