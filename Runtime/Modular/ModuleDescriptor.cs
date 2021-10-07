namespace Metamatic {
    using UnityEngine;
    using UnityEngine.AddressableAssets;
    using Sirenix.OdinInspector;
    using Metamatic.GameAccessManagment;

    [CreateAssetMenu(fileName = "ModuleDescriptor", menuName = "Dark Matter/Module Descriptor", order = 1)]
    public class ModuleDescriptor: ScriptableObject, IAccessable {
        [BoxGroup("Metadata")]
        public string title;
        [BoxGroup("Metadata")]
        public AssetReferenceSprite addressablePreview;
        public AssetReferenceGameObject addressablePrefab;
        public AssetReferenceScriptableSkin defaultAddressableSkin;
        public Configuration defaultConfiguration;
        public AssetReferenceScriptableSkin[] aviableSkins;
        public GameAccessConfig access = new GameAccessConfig();
        GameAccessConfig IAccessable.GetAccessConfig() {
            return access;
        }
    }
}