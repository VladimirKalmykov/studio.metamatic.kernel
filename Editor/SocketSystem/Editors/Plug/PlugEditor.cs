using Metamatic.Extensions;
using UnityEditor;
using UnityEngine.UIElements;

namespace Metamatic
{
    [CustomEditor(typeof(PlugLegacy), true)]
    [RequireEditorComponent(typeof(PlugGUIEditorComponent))]
    public class PlugEditor: ComposableEditor
    {
        protected override void OnCreateInspectorGUI(VisualElement root)
        {
            root.styleSheets.Add(this.LoadRelativeAsset<StyleSheet>("./style.uss"));
            root.AddToClassList("SocketGUIContainer");
        }
    }
}