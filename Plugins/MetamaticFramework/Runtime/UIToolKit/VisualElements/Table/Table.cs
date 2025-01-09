using System;
using Cysharp.Threading.Tasks;
using Metamatic.UIElements;
using UnityEngine.UIElements;

namespace Metamatic.UIToolKit
{
    public class Table<T>: AsyncVisualElement
    {
        public struct Column
        {
            public string Header;
            public Func<T, VisualElement> Accessor;
            public StyleLength Width;
        }
        
        private readonly Column[] m_Columns;
        private readonly VisualElement m_Body;
        private readonly ObservableList<T> m_Data;

        public Table(Column[] columns, ObservableList<T> data)
        {
            m_Columns = columns;
            m_Data = data;
            styleSheets.Add(UIToolKitCore.styleSheet);
            AddToClassList("MetamaticUIKit_Table");
            
            var header = new VisualElement();
            header.AddToClassList("MetamaticUIKit_Table_Header");
            
            foreach (var column in columns)
            {
                var headerCell = new Label(column.Header);
                headerCell.AddToClassList("MetamaticUIKit_Table_HeaderCell");
                headerCell.style.width = column.Width;
                header.Add(headerCell);
            }
            
            Add(header);
            
            var bodyContainer = new ScrollView(ScrollViewMode.Vertical);
            bodyContainer.AddToClassList("MetamaticUIKit_Table_BodyContainer");
            var body = m_Body = new VisualElement();
            body.AddToClassList("MetamaticUIKit_Table_Body");
            bodyContainer.Add(body);
            Add(bodyContainer);
        }
        public override async UniTask OnAttachedAsync(Signal detachSignal)
        {
            using var observeData = m_Data.WatchEachLifecycle(OnItemAddedAsync);
            await detachSignal;
        }

        private async UniTaskVoid OnItemAddedAsync(T item, int index, Signal removeSignal)
        {
            // TODO: add sort order according to index
            var row = new VisualElement();
            row.AddToClassList("MetamaticUIKit_Table_Row");
            foreach (var column in m_Columns)
            {
                var displayValue = column.Accessor(item);
                var cell = new VisualElement();
                cell.AddToClassList("MetamaticUIKit_Table_Cell");
                cell.style.width = column.Width;
                cell.Add(displayValue);
                row.Add(cell);
            }
            m_Body.Add(row);
            await removeSignal;
            row.RemoveFromHierarchy();
        }
    }
}