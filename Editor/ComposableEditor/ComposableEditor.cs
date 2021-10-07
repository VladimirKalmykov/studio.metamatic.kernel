using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using Metamatic;
using Sirenix.OdinInspector.Editor;


namespace Metamatic {
    public class ComposableEditor: OdinEditor {
        private Dictionary<RequireEditorComponentAttribute, EditorComponent> components = new Dictionary<RequireEditorComponentAttribute, EditorComponent>();
        private List<KeyValuePair<RequireEditorComponentAttribute, EditorComponent>>[] executeOrderList = new List<KeyValuePair<RequireEditorComponentAttribute, EditorComponent>>[0];
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
        }

        void Initialize() {
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

        VisualElement BuildInspectorGUI() {
            VisualElement root = new VisualElement();

            foreach(List<KeyValuePair<RequireEditorComponentAttribute, EditorComponent>> listItem in executeOrderList) {
                if (listItem != null) {
                    foreach(var item in listItem) {
                        root.Add(item.Value.CreateInspectorGUI());
                    }
                }
            }
            return root;
        }

        void ConnectEditorComponents(object target) {
            var attributes = target.GetType().GetCustomAttributes(true);
            foreach (object attribute in attributes)
            {
                RequireEditorComponentAttribute editorComponentAttribute = attribute as RequireEditorComponentAttribute;
                if (editorComponentAttribute != null) {
                    if (!components.ContainsKey(editorComponentAttribute)) {
                        if (editorComponentAttribute.componentType != null) {
                            /* Instantiate editor component */
                            EditorComponent newEditorComponent = Activator.CreateInstance(editorComponentAttribute.componentType) as EditorComponent;
                            newEditorComponent.editor = this;
                            newEditorComponent.target = this.target;
                            components.Add(editorComponentAttribute, newEditorComponent);
                        }
                    }
                }
            }
        }
    }
}