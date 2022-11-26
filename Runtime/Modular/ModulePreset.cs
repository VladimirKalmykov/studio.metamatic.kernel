namespace Metamatic {
    using System;
    using UnityEngine;
    using System.Threading.Tasks;

    [System.Serializable]
    public class ModulePreset {
        public ModuleDescriptor s_ModuleDescriptor = null;
        public event Action<ModulePreset> OnChange;
        public ModuleDescriptor moduleDescriptor {
            get {
                return s_ModuleDescriptor;
            }
            set {
                s_ModuleDescriptor = value;
                OnChange?.Invoke(this);
            }
        }

        // public AssetReferenceScriptableSkin s_Skin = null;
        // public AssetReferenceScriptableSkin skin {
        //     get {
        //         return s_Skin;
        //     }
        //     set {
        //         s_Skin = value;
        //         OnChange?.Invoke(this);
        //     }
        // }

        public Configuration s_Configuration = new Configuration();
        public Configuration configuration {
            get {
                return s_Configuration;
            }
            set {
                s_Configuration = value;
                OnChange?.Invoke(this);
            }
        }

        // public Task<GameObject> GetPrefabAsync() {
        //     return Metamatic.AddressableUtils.SafeLoadAssetAsync<GameObject>(moduleDescriptor.addressablePrefab);
        // }
        //
        // public Task<ScriptableSkin> GetSkinAsync() {
        //     return Metamatic.AddressableUtils.SafeLoadAssetAsync<ScriptableSkin>(skin ?? moduleDescriptor.defaultAddressableSkin);
        // }

        public ModulePreset Clone() {
            return new ModulePreset {
                moduleDescriptor = this.moduleDescriptor,
                // skin = this.skin,
                configuration = configuration.Clone()
            };
        }

        public Action Subscribe(Action<ModulePreset> handler) {
            handler(this);
            OnChange += handler;

            return () => {
                OnChange -= handler;
            };
        }

        public void Unsubscribe(Action<ModulePreset> handler) {
            OnChange -= handler;
        }
    }
}