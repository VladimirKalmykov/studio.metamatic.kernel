using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Metamatic
{
    public class VisualElementBackgroundHighligher: System.IDisposable
    {
        private VisualElement m_VisualElement;
        private Color m_HighlightColor;
        private Color m_DefaultColor;
        private UnityEngine.UIElements.StyleColor m_OriginalColor;
        private float m_Duration;
        private float m_ElapsedTime;
        private bool m_IsAnimating;
        private bool m_IsDisposed;
        public VisualElementBackgroundHighligher(VisualElement ve, Color highlightColor, float duration = 0.65f)
        {
            m_VisualElement = ve;
            m_HighlightColor = highlightColor;
            m_Duration = duration;
            m_DefaultColor = ve.resolvedStyle.backgroundColor;
            m_OriginalColor = ve.style.backgroundColor;
            ve.style.backgroundColor = highlightColor;

            m_ElapsedTime = 0.0f;
            m_IsAnimating = true;
            EditorApplication.update += AnimateColorBack;
        }
        
        private void AnimateColorBack()
        {
            if (!m_IsAnimating) return;

            m_ElapsedTime += Time.deltaTime;

            // Плавно изменяем цвет обратно к исходному
            m_VisualElement.style.backgroundColor = Color.Lerp(m_HighlightColor, m_DefaultColor, m_ElapsedTime / m_Duration);

            // Проверяем, закончилась ли анимация
            if (m_ElapsedTime >= m_Duration)
            {
                m_VisualElement.style.backgroundColor = Color.clear; 
                EditorApplication.update -= AnimateColorBack;
                m_IsAnimating = false;
            }
        }
        
        public void Dispose()
        {
            if (m_IsDisposed) return;
            m_IsDisposed = true;
            m_VisualElement.style.backgroundColor = Color.clear;
            EditorApplication.update -= AnimateColorBack;
        }
    }
}