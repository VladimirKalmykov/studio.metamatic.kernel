using UnityEngine.UIElements;

namespace Metamatic.UIToolKit
{
    public class BoxGroup: VisualElement
    {
        private Label m_HeaderLabel;
        private VisualElement m_Content;
        
        public string text
        {
            get => m_HeaderLabel.text;
            set => m_HeaderLabel.text = value;
        }
        
        // ReSharper disable once FunctionRecursiveOnAllPaths
        // ReSharper disable once ConvertToAutoProperty
        public override VisualElement contentContainer => m_Content;
        
        public BoxGroup(string text = "Header")
        {
            styleSheets.Add(UIToolKitCore.styleSheet);
            AddToClassList("MetamaticUIKit_BoxGroup");
            var header = new VisualElement();
            
            header.AddToClassList("MetamaticUIKit_BoxGroupHeader");
            
            m_HeaderLabel = new Label(text);
            header.Add(m_HeaderLabel);
            
            hierarchy.Add(header);
            
            m_Content = new VisualElement();
            m_Content.AddToClassList("MetamaticUIKit_BoxGroupContent");
            hierarchy.Add(m_Content);
        }
    }
}