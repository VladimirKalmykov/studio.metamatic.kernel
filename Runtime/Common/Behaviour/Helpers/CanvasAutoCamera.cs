using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Metamatic
{
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
