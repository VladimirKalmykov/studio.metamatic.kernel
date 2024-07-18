using UnityEditor;

namespace Metamatic {
    [CustomEditor(typeof(PluggableBehaviour), true)]
    [RequireEditorComponent(typeof(PluggableBehaviourEditorComponent))]
    public class PluggableBehaviourEditor: ComposableEditor {
        
    }
}