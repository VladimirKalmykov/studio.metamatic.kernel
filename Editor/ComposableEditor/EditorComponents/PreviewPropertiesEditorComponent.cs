using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using Metamatic;

namespace Metamatic {
    public class PreviewPropertiesEditorComponent: EditorComponent {
        class PropertyTypePreview: VisualElement {
            public PropertyTypePreview(FieldInfo fieldInfo): base() {
                this.style.paddingRight = new StyleLength(10);
                TextField label = new TextField();
                label.isReadOnly = true;
                label.SetEnabled(false);
                label.value = fieldInfo.FieldType.Name;
                this.Add(label);
            }
        }

        class PropertyValuePreview: VisualElement {
            public PropertyValuePreview(Type valueType, object value): base() {
                TextField label = new TextField();
                label.value = value.ToString();
                label.isReadOnly = true;
                label.SetEnabled(false);
                this.Add(label);
            }
        }

        readonly Color32 DEFAULT_COLOR = new Color32(168, 184, 182, byte.MaxValue);
        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = new VisualElement();

            /* Render fields */
            Label header = new Label("Fields");
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.marginBottom = new StyleLength(10f);
            root.Add(header);

            Type type = this.target.GetType();
            FieldInfo[] fields = type.GetFields();

            foreach(var propertyInfo in fields) {
                VisualElement box = new VisualElement();
                box.style.flexDirection = FlexDirection.Row;
                Label prefixedLabel = new Label(propertyInfo.Name);
                box.Add(prefixedLabel);
                prefixedLabel.style.width = EditorGUIUtility.labelWidth;

                /* Render type */
                VisualElement typeElement = new PropertyTypePreview(propertyInfo);
                typeElement.style.width = new StyleLength(120);
                typeElement.style.flexBasis = new StyleLength(120);
                typeElement.style.overflow = Overflow.Hidden;
                box.Add(typeElement);

                /* Render value */
                VisualElement valueElement = new PropertyValuePreview(propertyInfo.FieldType, propertyInfo.GetValue(this.target));
                valueElement.style.overflow = Overflow.Hidden; 
                valueElement.style.flexGrow = new StyleFloat(1);
                box.Add(valueElement);

                root.Add(box);
            }

            return root;
        }

        public PreviewPropertiesEditorComponent(ComposableEditor composableEditor) : base(composableEditor)
        {
        }
    }
}