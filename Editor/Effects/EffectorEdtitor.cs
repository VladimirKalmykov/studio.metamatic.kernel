using Metamatic;
using Metamatic.Effects;
using UnityEditor;
using UnityEngine;

namespace Metamatic.Effects
{
    [CustomEditor(typeof(Effector), true)]
    [RequireEditorComponent(typeof(EffectorHeaderEditorComponent))]
    [RequireEditorComponent(typeof(EffectorListEffectExecutionsEditorComponent))]
    public class EffectorEdtitor : ComposableEditor
    {
        
    }
}