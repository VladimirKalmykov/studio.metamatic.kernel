using System;
using UnityEngine;

namespace Metamatic {
    /// <summary>
    /// Describes the main color of the inspector window (use it for text color, or UI primary element color, etc)
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class InspectorColor: Attribute {
        public Type[] types;
        public Color color;
        public InspectorColor(float r = 0f, float g = 0f, float b = 0f, float a = 0f) {
            color = new Color(r, g, b, a);
        }
    }
}