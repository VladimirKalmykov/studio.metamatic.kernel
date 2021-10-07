using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Metamatic {
    
    [CustomEditor(typeof(NoIMGUIPluggableMonoBehaviour), true)]
    [RequireEditorComponent(typeof(InspectorPictureEditorComponent))]
    [RequireEditorComponent(typeof(InspectorDescriptionEditorComponent))]
    [RequireEditorComponent(typeof(MonoBehaviourPluginsEditorComponent))]
    public class NoIMGUIPluggableMonoBehaviourEditor: ComposableEditor {}
}