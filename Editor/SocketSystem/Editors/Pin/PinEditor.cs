using Metamatic;
using Metamatic.Effects;
using UnityEditor;
using UnityEngine;

namespace Metamatic
{
    [CustomEditor(typeof(Pin), true)]
    [RequireEditorComponent(typeof(PinConnectionsEditorComponent))]
    [RequireEditorComponent(typeof(StandartGUIEditorComponent))]
    public class PinEditor : ComposableEditor
    {
        
    }
}