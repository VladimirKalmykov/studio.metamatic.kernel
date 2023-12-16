using Metamatic.Extensions;
using UnityEditor;
using UnityEngine.UIElements;

namespace Metamatic
{
    [CustomEditor(typeof(Socket), true)]
    [RequireEditorComponent(typeof(SocketGUIEditorComponent))]
    public class SocketEditor: ComposableEditor
    {
        protected override void OnCreateInspectorGUI(VisualElement root)
        {
            root.styleSheets.Add(this.LoadRelativeAsset<StyleSheet>("./style.uss"));
            root.AddToClassList("SocketGUIContainer");
        }
    }
}