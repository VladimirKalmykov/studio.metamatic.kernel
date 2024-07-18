using System;
using UnityEditor;
using UnityEngine.UIElements;

namespace Metamatic
{
    public class FieldSet: VisualElement
    {
        private Label m_Label;
        private VisualElement m_Content;
        private VisualElement m_Indicator;
        private VisualElement m_Dot;

        private bool m_IsActive;
        public bool isActive
        {
            get => m_IsActive;
            set
            {
                m_IsActive = value;
                if (m_IsActive)
                {
                    m_Dot.AddToClassList("Connected");
                }
                else
                {
                    m_Dot.RemoveFromClassList("Connected");
                }
            }
        }
        private bool m_WithStatus;
        public bool withStatus
        {
            get => m_WithStatus;
            set
            {
                m_WithStatus = value;
                if (m_WithStatus)
                {
                    m_Indicator.style.display = DisplayStyle.Flex;
                }
                else
                {
                    m_Indicator.style.display = DisplayStyle.None;
                }
            }
        }
        
        private string m_Title;
        public string title
        {
            get => m_Title;
            set
            {
                m_Title = value;
                m_Label.text = value;
            }
        }
        private VisualElement[] m_ContentElements;
        public VisualElement[] content
        {
            get => m_ContentElements;
            set
            {
                m_ContentElements = value;
                m_Content.Clear();
                if (m_ContentElements != null)
                {
                    Array.ForEach(m_ContentElements, m_Content.Add);
                }
            }
        }
        public FieldSet(string title, VisualElement[] content = null, bool withStatus = false, bool isActive = false): base()
        {
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/studio.metamatic.kernel/Editor/Components/FieldSet/FieldSet.uxml");
            visualTree.CloneTree(this);
            
            AddToClassList("FieldSet");
            
            m_Label = this.Q<Label>("title");
            m_Indicator = this.Q<VisualElement>("indicator");
            m_Dot = this.Q<VisualElement>("indicator-dot");
            m_Content = this.Q<VisualElement>("content");
            
            this.title = title;
            this.content = content;
            this.isActive = isActive;
            this.withStatus = withStatus;
        }
    }
}