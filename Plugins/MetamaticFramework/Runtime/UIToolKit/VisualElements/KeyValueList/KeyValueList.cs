using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Metamatic;
using Metamatic.UIElements;
using UnityEngine.UIElements;

namespace Metamatic.UIToolKit
{
    public class KeyValueList: AsyncVisualElement
    {
        public class Item: VisualElement
        {
            public Label keyLabel { get; protected set; }

            public Item(string label)
            {
                AddToClassList("MetamaticUIKit_KeyValue");
                keyLabel = new Label(label);
                keyLabel.AddToClassList("MetamaticUIKit_Key");
                Add(keyLabel);
            }
        }
        public class Item<T>: Item where T: VisualElement
        {
            public T valueElement { get; private set; }

            public Item(string key, T value): base(key)
            {
                valueElement = value;
                valueElement.AddToClassList("MetamaticUIKit_KeyValueValue");
                Add(valueElement);
            }
        }
        
        private List<Item> m_Items = new();
        private Observable<Item> m_CurrentItemWithMaxLabelWidth = new();
        
        public KeyValueList()
        {
            // Connect styles from resoruces
            styleSheets.Add(UIToolKitCore.styleSheet);
            AddToClassList("MetamaticUIKit_KeyValueList");
        }
        
        public Item<T> AddItem<T>(string key, T value) where T: VisualElement
        {
            var item = new Item<T>(key, value);
            m_Items.Add(item);
            Add(item);
            return item;
        }
        
        public void RemoveItem(Item item)
        {
            m_Items.Remove(item);
            Remove(item);
        }

        public override async UniTask OnAttachedAsync(CancellationSignal detachSignal)
        {
            using var itemWithMaxWidthWatcher = m_CurrentItemWithMaxLabelWidth.WatchAsync(OnItemWithMaxWidthChanged);
            RegisterCallback<GeometryChangedEvent>(OnGeometryChange);
            await detachSignal;
            UnregisterCallback<GeometryChangedEvent>(OnGeometryChange);
        }
        
        private async UniTaskVoid OnItemWithMaxWidthChanged(Item item, CancellationSignal cancellationSignal)
        {
            if (item == null)
            {
                foreach (var i in m_Items)
                {
                    i.keyLabel.style.width = new StyleLength(StyleKeyword.Auto);
                }

                return;
            }
            foreach (var i in m_Items)
            {
                if (i == null) return;
                // Set the width of the key label to the width of the item with the max width
                // Except for the item with the max width
                i.keyLabel.style.width = i == item ? new StyleLength(StyleKeyword.Auto) : item.keyLabel.layout.width;
            }
            
            await cancellationSignal;
        }
        
        private void OnGeometryChange(GeometryChangedEvent evt)
        {
            var maxWidth = 0f;
            Item itemWithMaxWidth = null;
            foreach (var item in m_Items)
            {
                var width = item.keyLabel.layout.width;
                if (width > maxWidth)
                {
                    maxWidth = width;
                    itemWithMaxWidth = item;
                }
            }

            m_CurrentItemWithMaxLabelWidth.Value = itemWithMaxWidth;
        }
    }
}