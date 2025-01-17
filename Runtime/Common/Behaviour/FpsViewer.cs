namespace  Metamatic
{
    using UnityEngine;

    public class FpsViewer : MonoBehaviour
    {
        [Tooltip("Интервал пересчёта FPS в секундах")]
        public float updateInterval = 1.0f;

        private int m_Frames;
        private float m_AccumulatedTime;
        private float m_CurrentFps;

        private void Update()
        {
            m_AccumulatedTime += Time.deltaTime;
            m_Frames++;

            // Раз в updateInterval секунд пересчитываем fps
            if (m_AccumulatedTime >= updateInterval)
            {
                m_CurrentFps = m_Frames / m_AccumulatedTime;
                m_Frames = 0;
                m_AccumulatedTime = 0f;
            }
        }

        private void OnGUI()
        {
            // Ширина текста ~80, высота ~20, смещаемся от правого края экрана
            Rect rect = new Rect(Screen.width - 80, 10, 70, 20);
            GUI.Label(rect, m_CurrentFps.ToString("F1"));
        }
    }
}