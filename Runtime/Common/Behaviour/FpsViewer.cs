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
        }

        void OnGUI()
        {
            int w = Screen.width, h = Screen.height;

            GUIStyle style = new GUIStyle();

            Rect rect = new Rect(0, 0, w, h * 2 / 100);
            style.alignment = TextAnchor.UpperRight;
            style.fontSize = h * 2 / 100;
            style.normal.textColor = color;
            float msec = m_DeltaTime * 1000.0f;
            float fps = 1.0f / m_DeltaTime;
            string text = string.Format("{0:0.0} ms ({1:0.} fps)", msec, fps);
            GUI.Label(rect, text, style);
        }
    }
}