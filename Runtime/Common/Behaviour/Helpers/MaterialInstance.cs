namespace Metamatic {
    using System;
    using UnityEngine;

    [ExecuteAlways]
    public class MaterialInstance : MonoBehaviour
    {
        public int materialIndex;
        public Material defaultMaterial;
        private Material m_CachedDefaultMaterial;
        /* Once instantiate material instead of creating several instances will reduce GC work */
        private Material m_CachedMaterial;
        private Func<MaterialPropertyBlock, MaterialPropertyBlock> m_DefaultMaterialConfigurator;
        public Material material {
            get {
                return m_CachedMaterial;
            }
            set {
                InstantiateMaterial(value);
            }
        }
        private MaterialPropertyBlock m_MaterialPropertyBlock;
        public MaterialPropertyBlock sharedPropertyBlock {
            get {
                return m_MaterialPropertyBlock;
            }
        }
        public Func<MaterialPropertyBlock, MaterialPropertyBlock> sharedConfigurator {
            get {
                return m_DefaultMaterialConfigurator;
            }
        }
        private new MeshRenderer renderer;
        void Awake() {
            #if UNITY_EDITOR
            if (Application.isPlaying) {
            #endif
                renderer = GetComponent<MeshRenderer>();
                m_CachedDefaultMaterial = defaultMaterial == null ? renderer.materials[materialIndex] : defaultMaterial;
                m_MaterialPropertyBlock = new MaterialPropertyBlock();
                if (defaultMaterial != null) {
                    InstantiateMaterial(defaultMaterial);
                } else {
                    m_CachedMaterial = renderer.sharedMaterials[materialIndex];
                    ApplyProperties();
                }
            #if UNITY_EDITOR
            }
            #endif
        }
    #if UNITY_EDITOR
        void Start() {
            if (!Application.isPlaying) {
                renderer = GetComponent<MeshRenderer>();
                m_CachedDefaultMaterial = defaultMaterial == null ? renderer.sharedMaterials[materialIndex] : defaultMaterial;
                if (defaultMaterial != null) {
                    InstantiateMaterialInEditor(defaultMaterial);
                } else {
                    m_CachedMaterial = renderer.sharedMaterials[materialIndex];
                    ApplyProperties();
                }
            }
        }

        void InstantiateMaterialInEditor(Material material) {
            m_MaterialPropertyBlock = new MaterialPropertyBlock();
            Material[] nextMaterials = renderer.sharedMaterials;
            if (material.name != nextMaterials[materialIndex].name) {
                nextMaterials[materialIndex] = new Material(material);
                renderer.sharedMaterials = nextMaterials;
            }
            m_CachedMaterial = renderer.sharedMaterials[materialIndex];
            ApplyProperties();
        }
    #endif
        void InstantiateMaterial(Material material) {
            m_MaterialPropertyBlock = new MaterialPropertyBlock();
            m_CachedMaterial = Instantiate(material) as Material; 
            Material[] nextMaterials = renderer.sharedMaterials;
            nextMaterials[materialIndex] = m_CachedMaterial;
            renderer.materials = nextMaterials;
            ApplyProperties();
        }

        public void InitMaterial(Material material, Func<MaterialPropertyBlock, MaterialPropertyBlock> configurer) {
            if (material == null) {
                return;
            }
            #if UNITY_EDITOR
            if (!Application.isPlaying) {
                InstantiateMaterialInEditor(material);
            } else {
            #endif
                InstantiateMaterial(material);
            #if UNITY_EDITOR
            }
            #endif
            SetDefaultMaterial(m_CachedMaterial);
            SetDefaultMaterialСonfigurator(configurer);
            ApplyConfigurator();
        }

        public void SetDefaultMaterial(Material material) {
            m_CachedDefaultMaterial = material;
        }

        public void SetDefaultMaterialСonfigurator(Func<MaterialPropertyBlock, MaterialPropertyBlock> configurer) {
            m_DefaultMaterialConfigurator = configurer;
        }

        public void ApplyConfigurator() {
            if (m_DefaultMaterialConfigurator != null) {
                var nextPropertyBlock = m_DefaultMaterialConfigurator(sharedPropertyBlock);
                if (nextPropertyBlock != null) {
                    m_MaterialPropertyBlock = nextPropertyBlock;
                }
                ApplyProperties();
            }
        }

        public void ApplyConfigurator(Func<MaterialPropertyBlock, MaterialPropertyBlock> configurer) {
            if (configurer != null) {
                var nextPropertyBlock = configurer(sharedPropertyBlock);
                if (nextPropertyBlock != null) {
                    m_MaterialPropertyBlock = nextPropertyBlock;
                }
                ApplyProperties();
            }
        }

        public void ApplyProperties() {
            renderer.SetPropertyBlock(m_MaterialPropertyBlock, materialIndex);
        }

        public void ApplyProperties(MaterialPropertyBlock block) {
            renderer.SetPropertyBlock(block, materialIndex);
        }

        public void ResetMaterial() {
            #if UNITY_EDITOR
            if (Application.isPlaying) {
            #endif
                InstantiateMaterial(m_CachedDefaultMaterial);
            #if UNITY_EDITOR
            } else {
                InstantiateMaterialInEditor(m_CachedDefaultMaterial);
            }
            #endif
            ApplyConfigurator();
        }

        /* Returns MaterialInstance for specified index if exists otherwise create it */
        public static MaterialInstance GetMaterialInstance(MonoBehaviour target, int index) {
            var all = target.GetComponents<MaterialInstance>();
            foreach(var materialInstance in all) {
                if (materialInstance.materialIndex == index) {
                    return materialInstance;
                }
            }
            var newMaterialInstance = target.gameObject.AddComponent<MaterialInstance>();
            newMaterialInstance.materialIndex = index;
            return newMaterialInstance;
        }

        /* Returns MaterialInstance for specified index if exists otherwise create it */
        public static MaterialInstance GetMaterialInstance(GameObject target, int index) {
            var all = target.GetComponents<MaterialInstance>();
            foreach(var materialInstance in all) {
                if (materialInstance.materialIndex == index) {
                    return materialInstance;
                }
            }
            var newMaterialInstance = target.gameObject.AddComponent<MaterialInstance>();
            newMaterialInstance.materialIndex = index;
            return newMaterialInstance;
        }
    }

}