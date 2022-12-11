using Metamatic.Effects;

namespace Metamatic {
    using System;
    using UnityEngine.AddressableAssets;
    using Sirenix.OdinInspector;
    using Metamatic.GameAccessManagment;

    
    public abstract class ScriptableCustomizerEffect<TEffector, TProps> : ScriptableEffectWithSubEffects<TEffector, TProps>
    where TEffector: Effector
    where TProps : struct
    {
        [BoxGroup("Metadata")]
        public string title;
        [BoxGroup("Metadata")]
        public AssetReferenceSprite addressablePreview;
        public event Action OnSerializedFieldsChange;
        
        public override string GetEffectName()
        {
            return $"Skin: {title}";
        }

        #if UNITY_EDITOR
        private void OnValidate() {
            OnSerializedFieldsChange?.Invoke();
        }
        #endif
    }
}