using System;
using UnityEngine.UIElements;

namespace Metamatic.Framework
{
    public class Form: VisualElement
    {
        private VisualElement m_Content;
        private VisualElement m_Footer;
        private Button m_SubmitButton;
        private Label m_FooterMessage;

        private bool m_IsValid;
        public override VisualElement contentContainer => m_Content;

        public bool isValid
        {
            get => m_IsValid;
            set
            {
                m_IsValid = value;
                Update();
            }
        }
        
        public Form(Action submitAction, string submitText, bool isValid = true) // 123
        {
            m_IsValid = isValid;
            AddToClassList("Form");
            
            m_Content = new VisualElement();
            m_Content.AddToClassList("FormContent");
            
            m_Footer = new VisualElement();
            m_Footer.AddToClassList("FormFooter");
            
            /* Create submit button */
            m_SubmitButton = new Button(() =>
            {
                if (!m_IsValid) return;
                submitAction?.Invoke();
            });
            m_SubmitButton.text = submitText;
            m_SubmitButton.enabledSelf = isValid;
            
            m_FooterMessage = new Label("Footer message");
            m_FooterMessage.AddToClassList("FormMessage");
            m_FooterMessage.style.visibility = Visibility.Hidden;
            
            m_Footer.Add(m_FooterMessage);
            m_Footer.Add(m_SubmitButton);
            
            hierarchy.Add(m_Content);
            hierarchy.Add(m_Footer);
        }
        
        public void AddContent(VisualElement element)
        {
            m_Content.Add(element);
        }
        
        public void ClearContent()
        {
            m_Content.Clear();
        }
        
        public void SetMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                m_FooterMessage.style.visibility = Visibility.Hidden;
                return;
            }
            m_FooterMessage.text = message;
            m_FooterMessage.style.visibility = Visibility.Visible;
        }
        
        public void AddFooter(VisualElement element)
        {
            m_Footer.Add(element);
        }
        
        private void Update()
        {
            m_SubmitButton.enabledSelf = m_IsValid;
        }
    }
}