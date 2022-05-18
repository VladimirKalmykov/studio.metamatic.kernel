using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using Metamatic;
using Sirenix.OdinInspector.Editor;
using Random = UnityEngine.Random;


namespace Metamatic {
    public class ComposableEditor: OdinEditor {
        private Dictionary<RequireEditorComponentAttribute, EditorComponent> components = new Dictionary<RequireEditorComponentAttribute, EditorComponent>();
        private List<KeyValuePair<RequireEditorComponentAttribute, EditorComponent>>[] executeOrderList = new List<KeyValuePair<RequireEditorComponentAttribute, EditorComponent>>[0];
        private bool m_Enabled = false;
        public override VisualElement CreateInspectorGUI()
        {
            Initialize();
            
            return BuildInspectorGUI();
        }

        protected override void OnEnable(){
            var remainingBuggedEditors  = Editor.FindObjectsOfType<ComposableEditor>();
            foreach(var editor in remainingBuggedEditors) {
                if(editor == this){
                    continue;
                }
                Editor.DestroyImmediate(editor);
            }//end foreach

            if (!m_Enabled)
            {
                m_Enabled = true;
                foreach (var pair in components)
                {
                    pair.Value.OnEnable();
                }
            }
        }

        protected override void OnDisable()
        {
            if (m_Enabled)
            {
                m_Enabled = false;
                foreach (var pair in components)
                {
                    pair.Value.OnDisable();
                }
            }
        }

        private void Initialize() {
            /* Connect components */
            ConnectEditorComponents(this);
            ConnectEditorComponents(this.target);
            
            foreach (var item in components) {
                if (item.Key.order >= executeOrderList.Length) {
                    Array.Resize(ref executeOrderList, item.Key.order + 1);
                }

                if (executeOrderList[item.Key.order] == null) {
                    executeOrderList[item.Key.order] = new List<KeyValuePair<RequireEditorComponentAttribute, EditorComponent>>();
                }

                executeOrderList[item.Key.order].Add(item);
            }
        }

        private VisualElement BuildInspectorGUI() {
            var root = new VisualElement();

            foreach(var listItem in executeOrderList) {
                if (listItem != null) {
                    foreach(var item in listItem)
                    {
                        var component = item.Value;
                        root.Add(component);
                    }
                }
            }
            return root;
        }

        private void ConnectEditorComponents(object target) {
            var attributes = target.GetType().GetCustomAttributes(true);
            foreach (var attribute in attributes)
            {
                var editorComponentAttribute = attribute as RequireEditorComponentAttribute;
                if (editorComponentAttribute != null) {
                    if (!components.ContainsKey(editorComponentAttribute)) {
                        if (editorComponentAttribute.componentType != null) {
                            /* Instantiate editor component */
                            var ctor = editorComponentAttribute.componentType.GetConstructor(new[]
                                {typeof(ComposableEditor)});
                            var newEditorComponent = ctor.Invoke(new object[]{this}) as EditorComponent;
                            // EditorComponent newEditorComponent = Activator.CreateInstance(editorComponentAttribute.componentType, this) as EditorComponent;
                            components.Add(editorComponentAttribute, newEditorComponent);
                            newEditorComponent.OnEnable();
                        }
                    }
                }
            }
        }
    }
}