using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Metamatic.UIToolKit
{
    public class MultiColumnsView: VisualElement
    {
        private readonly List<VisualElement> m_Columns = new List<VisualElement>();
        public MultiColumnsView()
        {
            styleSheets.Add(UIToolKitCore.styleSheet);
            AddToClassList("MetamaticUIKit_MultiColumnsView");
        }
    }
}