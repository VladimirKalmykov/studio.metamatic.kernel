
using UnityEngine.UIElements;

namespace Metamatic.UIToolKit.VisualElements
{
    public class ToggleButton: Button
    {
        private bool m_Value;

        public bool value
        {
            get => m_Value;
            set
            {
                if (m_Value == value) return;
                m_Value = value;
                if (m_Value)
                {
                    AddToClassList("Active");
                }
                else
                {
                    RemoveFromClassList("Active");
                }
            }
        }
        
        public ToggleButton(): base()
        {
            AddToClassList("MetamaticUIKit_ToggleButton");
        }
    }
}