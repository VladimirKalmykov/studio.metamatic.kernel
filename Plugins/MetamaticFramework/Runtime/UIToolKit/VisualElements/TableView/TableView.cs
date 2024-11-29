using System;
using Cysharp.Threading.Tasks;
using Metamatic.UIElements;
using UnityEngine.UIElements;

namespace Metamatic.UIToolKit
{
    public class TableView<T>: AsyncVisualElement
    {
        public class Col
        {
            public enum WidthType
            {
                Relative,
                Fixed
            }
            public string title;
            public WidthType widthType;
            public float width;
            public Func<T, VisualElement> renderer;
            
            public Col(string title, WidthType widthType, float width, Func<T, VisualElement> renderer)
            {
                this.title = title;
                this.widthType = widthType;
                this.width = width;
                this.renderer = renderer;
            }
        }
        
        private Col[] m_Cols;
        private float m_TotalRelativeWidth;
        private float m_TotalFixedWidth;
        private VisualElement[] m_HeaderCols;
        private VisualElement m_Body;
        private readonly Observable<ObservableList<T>> m_BoundList = new();
        
        
        public TableView(Col[] cols)
        {
            m_Cols = cols;
            
            styleSheets.Add(UIToolKitCore.styleSheet);
            AddToClassList("MetamaticUIKit_TableView");
            
            // Add header
            var header = new VisualElement();
            header.AddToClassList("MetamaticUIKit_TableView_Header");
            CreateCols(header);
            
            // Add body
            m_Body = new VisualElement();
            m_Body.AddToClassList("MetamaticUIKit_TableView_Body");
            
            Add(header);
            Add(m_Body);
            
            OnUpdateGeometry();
        }
        
        private VisualElement AddRow(T item)
        {
            var width = contentRect.width;
            var leftWidth = width - m_TotalFixedWidth;
            var step = leftWidth / m_TotalRelativeWidth;
            
            var row = new VisualElement();
            row.AddToClassList("MetamaticUIKit_TableView_Row");
            
            for (var i = 0; i < m_Cols.Length; i++)
            {
                var col = m_Cols[i];
                var cellElement = new VisualElement();
                cellElement.AddToClassList("MetamaticUIKit_TableView_Cell");
                var cellContent = col.renderer(item);
                cellElement.Add(cellContent);
                
                switch (col.widthType)
                {
                    case Col.WidthType.Fixed:
                        cellElement.style.width = col.width;
                        break;
                    case Col.WidthType.Relative:
                        cellElement.style.width = col.width * step;
                        break;
                }
                
                row.Add(cellElement);
            }
            m_Body.Add(row);
            return row;
        }
        
        public void RemoveRow(VisualElement row)
        {
            m_Body.Remove(row);
        }
        
        public void Bind(ObservableList<T> list)
        {
            m_BoundList.Value = list;
        }

        private void CreateCols(VisualElement header)
        {
            m_TotalRelativeWidth = 0f;
            m_HeaderCols = new VisualElement[m_Cols.Length];
            for (var i = 0; i < m_Cols.Length; i++)
            {
                var col = m_Cols[i];
                var headerCol = new VisualElement();
                headerCol.AddToClassList("MetamaticUIKit_TableView_HeaderCol");
                switch (col.widthType)
                {
                    case Col.WidthType.Fixed:
                        headerCol.style.width = col.width;
                        m_TotalFixedWidth += col.width;
                        break;
                    case Col.WidthType.Relative:
                        m_TotalRelativeWidth += col.width;
                        break;
                }
                headerCol.Add(new Label(col.title));
                m_HeaderCols[i] = headerCol;
                header.Add(headerCol);
            }
        }
        
        private void OnUpdateGeometry()
        {
            var width = contentRect.width;
            var leftWidth = width - m_TotalFixedWidth;
            var step = leftWidth / m_TotalRelativeWidth;
            for (var i = 0; i < m_Cols.Length; i++)
            {
                var col = m_Cols[i];
                switch (col.widthType)
                {
                    case Col.WidthType.Relative:
                        m_HeaderCols[i].style.width = col.width * step;
                        break;
                }
            }
            
            // Also update each row
            foreach (var row in m_Body.Children())
            {
                var children = row.Children();
                var index = 0;
                foreach (var cell in children)
                {
                    var col = m_Cols[index];
                    switch (col.widthType)
                    {
                        case Col.WidthType.Relative:
                            cell.style.width = col.width * step;
                            break;
                    }
                    index++;
                }
            }
        }
        
        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            OnUpdateGeometry();
        }

        public override async UniTask OnAttachedAsync(CancellationSignal detachSignal)
        {
            // Start track container width
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            using var watchBoundList = m_BoundList.WatchAsync(OnBoundListChanged);
            await detachSignal;
            UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }
        
        private async UniTaskVoid OnBoundListChanged(ObservableList<T> list, CancellationSignal signal)
        {
            m_Body.Clear();
            if (list != null)
            {
                using var watchList = list.WatchEachLifecycle(OnListItemChanged);
                await signal;
            }
        }
        
        private async UniTaskVoid OnListItemChanged(T item, int index, CancellationSignal signal)
        {
            var rowElement = AddRow(item);
            await signal;
            RemoveRow(rowElement);
        }
    }
}