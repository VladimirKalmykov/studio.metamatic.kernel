using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;

namespace Metamatic {
    public class ImguiEditorComponent: EditorComponent {
        public override VisualElement CreateInspectorGUI() {
            VisualElement root = new UnityEngine.UIElements.IMGUIContainer(() => {
                if (editor.target != null) {
                    editor.OnInspectorGUI();
                }
            });
            return root;
        }

        public ImguiEditorComponent(ComposableEditor composableEditor) : base(composableEditor)
        {
        }
    }
}

        