namespace Metamatic {
    using UnityEngine;

    [CreateAssetMenu(fileName = "ScriptableModulePreset", menuName = "Metamatic/Module Preset", order = 2)]
    public class ScriptableModulePreset: ScriptableObject {
        public ModulePreset modulePreset;
    }
}