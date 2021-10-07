namespace Metamatic {
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.AddressableAssets;
    using Sirenix.OdinInspector;

    [System.Serializable]
    public class Configuration {
        public abstract class ConfigurationSerializedDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
        {
            [SerializeField, HideInInspector]
            private List<TKey> keyData = new List<TKey>();
            
            [SerializeField, HideInInspector, SerializeReference]
            private List<TValue> valueData = new List<TValue>();

            void ISerializationCallbackReceiver.OnAfterDeserialize()
            {
                this.Clear();
                for (int i = 0; i < this.keyData.Count && i < this.valueData.Count; i++)
                {
                    this[this.keyData[i]] = this.valueData[i];
                }
            }

            void ISerializationCallbackReceiver.OnBeforeSerialize()
            {
                this.keyData.Clear();
                this.valueData.Clear();

                foreach (var item in this)
                {
                    this.keyData.Add(item.Key);
                    this.valueData.Add(item.Value);
                }
            }
        }
        [System.Serializable]
        public class PrefabDictionary: ConfigurationSerializedDictionary<string, GameObject> {}
        [System.Serializable]
        public class IntDictionary: ConfigurationSerializedDictionary<string, int> {}
        [System.Serializable]
        public class FloatDictionary: ConfigurationSerializedDictionary<string, float> {}
        [System.Serializable]
        public class ColorDictionary: ConfigurationSerializedDictionary<string, Color> {}
        [System.Serializable]
        public class SpriteDictionary: ConfigurationSerializedDictionary<string, Sprite> {}
        [System.Serializable]
        public class Vector4Dictionary: ConfigurationSerializedDictionary<string, Vector4> {}
        [System.Serializable]
        public class MaterialDictionary: ConfigurationSerializedDictionary<string, Material> {}
        [System.Serializable]
        public class ConfigurationDictionary: ConfigurationSerializedDictionary<string, Configuration> {}
        [System.Serializable]
        public class AssetReferenceDictionary: ConfigurationSerializedDictionary<string, AssetReference> {}
        public class ModulePresetDictionary: ConfigurationSerializedDictionary<string, ModulePreset> {}

        public PrefabDictionary prefabs = new PrefabDictionary();
        public IntDictionary integers = new IntDictionary();
        public FloatDictionary floats = new FloatDictionary();
        public ColorDictionary colors = new ColorDictionary();
        public SpriteDictionary sprites = new SpriteDictionary();
        public Vector4Dictionary vectors = new Vector4Dictionary();
        public MaterialDictionary materials = new MaterialDictionary();
        public ConfigurationDictionary configurations = new ConfigurationDictionary();
        public ModulePresetDictionary modulePresets = new ModulePresetDictionary();

        /* Because of bug with Odin and AssetReference property drawer assetReferences should be hidden in the inspector: https://bitbucket.org/sirenix/odin-inspector/issues/669/support-for-assetreference-drawers */
        [HideInInspector]
        public AssetReferenceDictionary assetReferences = new AssetReferenceDictionary();

        public event Action OnChange;

        public GameObject GetPrefab(string code, GameObject byDefault = null) {
            GameObject value;
            if (prefabs.TryGetValue(code, out value)) {
                return value;
            }
            return byDefault;
        }

        public int GetInt(string code, int byDefault = default) {
            int value;
            if (integers.TryGetValue(code, out value)) {
                return value;
            }
            return byDefault;
        }

        public float GetFloat(string code, float byDefault = default) {
            float value;
            if (floats.TryGetValue(code, out value)) {
                return value;
            }
            return byDefault;
        }

        public Color GetColor(string code, Color byDefault = default) {
            Color value;
            if (colors.TryGetValue(code, out value)) {
                return value;
            }
            return byDefault;
        }

        public Sprite GetSprite(string code, Sprite byDefault = default) {
            Sprite value = null; 
            if (sprites.TryGetValue(code, out value)) {
                return value;
            }
            return byDefault;
        }

        public Vector4 GetVector(string code, Vector4 byDefault = default) {
            Vector4 value;
            if (vectors.TryGetValue(code, out value)) {
                return value;
            }
            return byDefault;
        }

        public Material GetMaterial(string code, Material byDefault = default) {
            Material value;
            if (materials.TryGetValue(code, out value)) {
                return value;
            }
            return byDefault;
        }

        public Configuration GetConfiguration(string code, Configuration byDefault = default) {
            Configuration value;
            if (configurations.TryGetValue(code, out value)) {
                return value;
            }
            return byDefault;
        }

        public AssetReference GetAssetReference(string code, AssetReference byDefault = default) {
            AssetReference value;
            if (assetReferences.TryGetValue(code, out value)) {
                return value;
            }
            return byDefault;
        }

        public ModulePreset GetModulePreset(string code, ModulePreset byDefault = default) {
            ModulePreset value;
            if (modulePresets.TryGetValue(code, out value)) {
                return value;
            }
            return byDefault;
        }

        public void SetColor(string code, Color value) {
            colors[code] = value;
            OnChange?.Invoke();
        }

        public void SetModulePreset(string code, ModulePreset value) {
            modulePresets[code] = value;
            OnChange?.Invoke();
        }

        public void Assign(Configuration other) {
            if (other == null) {
                return;
            }
            /* Assign prefabs */
            foreach(var prefab in other.prefabs) {
                prefabs[prefab.Key] = prefab.Value;
            }
            /* Assign integers */
            foreach(var integer in other.integers) {
                integers[integer.Key] = integer.Value;
            }
            /* Assign floats */
            foreach(var item in other.floats) {
                floats[item.Key] = item.Value;
            }
            /* Assign colors */
            foreach(var item in other.colors) {
                colors[item.Key] = item.Value;
            }
            /* Assign sproites */
            foreach(var item in other.sprites) {
                sprites[item.Key] = item.Value;
            }
            /* Assign sproites */
            foreach(var item in other.vectors) {
                vectors[item.Key] = item.Value;
            }
            /* Assign materials */
            foreach(var item in other.materials) {
                materials[item.Key] = item.Value;
            }
            /* Assign configurations */
            foreach(var item in other.configurations) {
                configurations[item.Key] = item.Value;
            }
            /* Assign assetReferences */
            foreach(var item in other.assetReferences) {
                assetReferences[item.Key] = item.Value;
            }
            /* Assign modulePresets */
            foreach(var item in other.modulePresets) {
                modulePresets[item.Key] = item.Value;
            }
            OnChange?.Invoke();
        }

        public void Clear(bool silent = false) {
            prefabs.Clear();
            integers.Clear();
            floats.Clear();
            colors.Clear();
            sprites.Clear();
            vectors.Clear();
            materials.Clear();
            configurations.Clear();
            assetReferences.Clear();
            modulePresets.Clear();

            if (!silent) {
                OnChange?.Invoke();
            }
        }

        public Configuration Clone() {
            var newConfiguration = new Configuration();
            newConfiguration.Assign(this);
            return newConfiguration;
        }
    }
}