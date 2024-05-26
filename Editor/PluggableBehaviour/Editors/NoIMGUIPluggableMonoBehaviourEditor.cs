using UnityEditor;

namespace Metamatic {
    
    [CustomEditor(typeof(NoIMGUIPluggableMonoBehaviour), true)]
    [RequireEditorComponent(typeof(InspectorPictureEditorComponent))]
    [RequireEditorComponent(typeof(InspectorDescriptionEditorComponent))]
    [RequireEditorComponent(typeof(MonoBehaviourPluginsEditorComponent))]
    public class NoIMGUIPluggableMonoBehaviourEditor: ComposableEditor {}
}