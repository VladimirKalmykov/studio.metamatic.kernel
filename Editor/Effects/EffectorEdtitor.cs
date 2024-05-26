using UnityEditor;

namespace Metamatic.Effects
{
    [CustomEditor(typeof(Effector), true)]
    [RequireEditorComponent(typeof(EffectorHeaderEditorComponent))]
    [RequireEditorComponent(typeof(EffectorListEffectExecutionsEditorComponent))]
    [RequireEditorComponent(typeof(StandartGUIEditorComponent))]
    public class EffectorEdtitor : ComposableEditor
    {
        
    }
}