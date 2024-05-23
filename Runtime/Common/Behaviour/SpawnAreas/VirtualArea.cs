namespace Metamatic
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public abstract class VirtualArea : MonoBehaviour
    {
        [SerializeField] private Color m_DebugColor = Color.green;
        public bool overlapped { get; set; }= false;
        public Color debugColor => m_DebugColor;
        
    }
}