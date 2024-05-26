using UnityEngine;
using UnityEngine.UIElements;

namespace Metamatic {
    public class InspectorDescriptionEditorComponent: EditorComponent {
        readonly Color32 DEFAULT_COLOR = new Color32(168, 184, 182, byte.MaxValue);
        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = new VisualElement();

            /* Handle target attibutes */
            Component target = (Component)this.target;
            var attributes = target.GetType().GetCustomAttributes(true);
            foreach (object attribute in attributes)
            {
                InspectorDescriptionAttribute description = attribute as InspectorDescriptionAttribute;
                if (description != null) {
                    string text = description.text;
                    if (text.Length > 0) {
                        Label label = new Label(text);
                        label.style.marginBottom = new StyleLength(12f);
                        label.style.color = description.color != default ? description.color : (Color)DEFAULT_COLOR;
                        label.style.whiteSpace = WhiteSpace.Normal;
                        label.style.paddingTop = new StyleLength(10);
                        label.style.paddingRight = new StyleLength(10);
                        label.style.paddingBottom = new StyleLength(10);
                        label.style.paddingLeft = new StyleLength(10);
                        label.style.backgroundColor = new StyleColor(new Color32(62, 69, 68, byte.MaxValue));
                        label.style.borderTopLeftRadius = new StyleLength(4);
                        label.style.borderTopRightRadius = new StyleLength(4);
                        label.style.borderBottomRightRadius = new StyleLength(4);
                        label.style.borderBottomLeftRadius = new StyleLength(4);
                        root.Add(label);
                    }
                }
            }
            
            return root;
        }

        public InspectorDescriptionEditorComponent(ComposableEditor composableEditor) : base(composableEditor)
        {
        }
    }
}