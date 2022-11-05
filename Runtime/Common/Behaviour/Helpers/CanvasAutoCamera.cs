using UnityEngine;

namespace Metamatic
{
    /// <summary>
    /// Set cancas worldCamera to current camera
    /// </summary>
    public class CanvasAutoCamera : MonoBehaviour
    {
        private Canvas m_Canvas;

        private void OnEnable()
        {
            m_Canvas = GetComponent<Canvas>();
            m_Canvas.worldCamera = Camera.main;
        }
    }
}
