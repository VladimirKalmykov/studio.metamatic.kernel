namespace Metamatic {
    using System;
    using UnityEngine;
    using UnityEngine.AddressableAssets;
    using Sirenix.OdinInspector;
    using Metamatic.GameAccessManagment;

    public abstract class ScriptableSkin : ScriptableObject, IAccessable
    {
        [BoxGroup("Metadata")]
        public string title;
        [BoxGroup("Metadata")]
        public AssetReferenceSprite addressablePreview;
        public event Action OnSerializedFieldsChange;
        [BoxGroup("Access Control")]
        public GameAccessConfig access = new GameAccessConfig();
        GameAccessConfig IAccessable.GetAccessConfig() {
            return access;
        }
        public virtual void Apply(GameObject target) {
            
        }

        #if UNITY_EDITOR
        private void OnValidate() {
            OnSerializedFieldsChange?.Invoke();
        }
        #endif
    }
}