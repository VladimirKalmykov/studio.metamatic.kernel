using Metamatic.DevTools;

namespace  Metamatic
{
    using UnityEngine;

    public class FpsViewer : MonoBehaviour
    {
        public Color color = Color.black;
        float m_DeltaTime = 0.0f;

        void Update()
        {
            m_DeltaTime += (Time.unscaledDeltaTime - m_DeltaTime) * 0.1f;
            float msec = m_DeltaTime * 1000.0f;
            float fps = 1.0f / m_DeltaTime;
            string text = string.Format("{0:0.0} ms ({1:0.} fps)", msec, fps);
            UILog.Add("FPS", text);
        }
    }
}