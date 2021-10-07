using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Metamatic {
    
    [CustomEditor(typeof(PluggableMonoBehaviour), true)]
    [RequireEditorComponent(typeof(InspectorPictureEditorComponent))]
    [RequireEditorComponent(typeof(InspectorDescriptionEditorComponent))]
    [RequireEditorComponent(typeof(ImguiEditorComponent))]
    [RequireEditorComponent(typeof(MonoBehaviourPluginsEditorComponent))]
    public class PluggableMonoBehaviourEditor: ComposableEditor {}
}