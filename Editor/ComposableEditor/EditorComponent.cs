using System;
using UnityEditor;
using UnityEngine.UIElements;

namespace Metamatic {
    public abstract class EditorComponent {
        public UnityEngine.Object target {
            get; set;
        }
        public Editor editor {
            get; set;
        }
        public virtual VisualElement CreateInspectorGUI() {
            return new VisualElement();
        }
    }
}