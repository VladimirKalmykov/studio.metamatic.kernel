using System;
using UnityEditor;
using UnityEngine.UIElements;

namespace Metamatic {
    public abstract class EditorComponent: VisualElement
    {
        public readonly UnityEngine.Object target;
        public readonly ComposableEditor composableEditor;
        public readonly Editor editor;
        public SerializedObject serializedObject => editor.serializedObject;

        public virtual VisualElement CreateInspectorGUI()
        {
            return null;
        }

        public EditorComponent(ComposableEditor composableEditor)
        {
            this.composableEditor = composableEditor;
            editor = (Editor)composableEditor;
            target = composableEditor.target;
            
            var el = CreateInspectorGUI();
            if (el != null)
            {
                Add(el);
            }
        }

        public virtual void OnEnable()
        {
            
        }

        public virtual void OnDisable()
        {
            
        }
    }
}