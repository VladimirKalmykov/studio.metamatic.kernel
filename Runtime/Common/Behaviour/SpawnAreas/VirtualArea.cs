namespace Metamatic
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public abstract class VirtualArea : MonoBehaviour
    {
        [SerializeField] private bool m_IsContainer = false;
        [SerializeField] private Color m_DebugColor = Color.green;
        public Color debugColor => m_DebugColor;
        public bool isContainer => m_IsContainer;
        
    }
}