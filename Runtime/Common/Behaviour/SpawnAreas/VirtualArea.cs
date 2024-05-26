namespace Metamatic
{
    using UnityEngine;

    public abstract class VirtualArea : MonoBehaviour
    {
        [SerializeField] private bool m_AutoTrack = false;
        [SerializeField] private bool m_IsContainer = false;
        [SerializeField] private Color m_DebugColor = Color.green;
        public Color debugColor => m_DebugColor;
        public bool isContainer => m_IsContainer;
        public bool autoTrack => m_AutoTrack;
        
    }
}