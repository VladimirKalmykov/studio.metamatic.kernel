using Metamatic.Extensions;
using UnityEngine.UIElements;

namespace Metamatic.Effects
{
    public class PinHeaderEditorComponent : EditorComponent
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            root.styleSheets.Add(this.editor.LoadRelativeAsset<StyleSheet>("./styles.uss"));
            root.AddToClassList("HeaderPicture");
            return root;
        }

        public PinHeaderEditorComponent(ComposableEditor composableEditor) : base(composableEditor)
        {
        }
    }
}