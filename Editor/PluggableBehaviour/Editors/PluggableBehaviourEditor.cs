using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Metamatic {
    [CustomEditor(typeof(PluggableBehaviour), true)]
    [RequireEditorComponent(typeof(PluggableBehaviourEditorComponent))]
    public class PluggableBehaviourEditor: ComposableEditor {
        
    }
}