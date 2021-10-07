using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;

namespace Metamatic {
    public class StandartGUIEditorComponent: EditorComponent {
        public override VisualElement CreateInspectorGUI() {
            VisualElement root = new VisualElement();
            SerializedObject serializedObject = new SerializedObject(this.target);
            /* Render other serialazible fields */
            SerializedProperty prop = serializedObject.GetIterator();
            if (prop.NextVisible(true)) {
                do {
                    if (prop.name != "m_Script" && !prop.name.StartsWith("_UNITY_EDITOR")) {
                        PropertyField propertyField = new PropertyField();
                        propertyField.label = prop.name;
                        propertyField.Bind(serializedObject);
                        propertyField.bindingPath = prop.name;
                        propertyField.style.marginBottom = new StyleLength(2f);

                        root.Add(propertyField);
                    }
                } while (prop.NextVisible(false));
            }

            return root;
        }
    }
}

        