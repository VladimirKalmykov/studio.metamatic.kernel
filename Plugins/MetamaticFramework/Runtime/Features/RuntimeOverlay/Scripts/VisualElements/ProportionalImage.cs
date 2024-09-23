using UnityEngine.UIElements;

namespace Metamatic.Framework
{
    // Добавляем атрибут UxmlElement для класса
    [UxmlElement]
    public partial class ProportionalImage : VisualElement
    {
        public enum LeadingAxis
        {
            Width,
            Height
        }

        private LeadingAxis m_LeadingAxis = LeadingAxis.Width;
        private float m_Ratio = 1f;

        public LeadingAxis leadingAxis
        {
            get => m_LeadingAxis;
            set
            {
                m_LeadingAxis = value;
                UpdateGeometry();
            }
        }

        public float ratio
        {
            get => m_Ratio;
            set
            {
                m_Ratio = value;
                UpdateGeometry();
            }
        }

        public ProportionalImage()
        {
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        public ProportionalImage(LeadingAxis leadingAxis, float ratio = 1f)
        {
            m_LeadingAxis = leadingAxis;
            m_Ratio = ratio;

            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            UpdateGeometry();
        }

        private void UpdateGeometry()
        {
            if (m_LeadingAxis == LeadingAxis.Width)
            {
                style.height = resolvedStyle.width * m_Ratio;
            }
            else
            {
                style.width = resolvedStyle.height * m_Ratio;
            }
        }
    }
}