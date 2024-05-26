using UnityEditor;

namespace Metamatic {
    
    [CustomEditor(typeof(PluggableMonoBehaviour), true)]
    [RequireEditorComponent(typeof(InspectorPictureEditorComponent))]
    [RequireEditorComponent(typeof(InspectorDescriptionEditorComponent))]
    [RequireEditorComponent(typeof(ImguiEditorComponent))]
    [RequireEditorComponent(typeof(MonoBehaviourPluginsEditorComponent))]
    public class PluggableMonoBehaviourEditor: ComposableEditor {}
}