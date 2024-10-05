using UnityEngine.UIElements;

namespace Metamatic.Framework
{
    public class Field: VisualElement
    {
        private readonly Label m_Label;
        private readonly VisualElement m_Content;
        
        public string labelText
        {
            get => m_Label.text;
            set => m_Label.text = value;
        }
        
        public Field(string labelText)
        {
            AddToClassList("Field");
            
            m_Label = new Label("Label");
            m_Label.AddToClassList("FieldLabel");
            m_Label.text = labelText;
            
            m_Content = new VisualElement();
            m_Content.AddToClassList("FieldContent");
            
            Add(m_Label);
            Add(m_Content);
        }
        
        public new void AddContent(VisualElement element)
        {
            m_Content.Add(element);
        }
    }
}