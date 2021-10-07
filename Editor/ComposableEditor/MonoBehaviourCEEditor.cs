using UnityEngine;
using UnityEditor;

namespace Metamatic {
    [CustomEditor(typeof(MonoBehaviourCE), true)]
    [RequireEditorComponent(typeof(InspectorPictureEditorComponent))]
    [RequireEditorComponent(typeof(InspectorDescriptionEditorComponent))]
    [RequireEditorComponent(typeof(ImguiEditorComponent))]
    public class MonoBehaviourCEEditor: ComposableEditor {}
}