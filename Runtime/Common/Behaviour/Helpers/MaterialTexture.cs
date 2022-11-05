using UnityEngine;

namespace Metamatic
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Events;

    [RequireComponent(typeof(MaterialInstance))]
    public class MaterialTexture : MonoBehaviour
    {
        public int materialIndex = 0;
        public string key = "_MainTex";
        public Texture2D texture = default;
        private new Renderer renderer;
        private MaterialInstance m_MaterialInstance;
        // Start is called before the first frame update
        void Start()
        {
            renderer = GetComponent<Renderer>();
            var name = renderer.materials[materialIndex].name;
            m_MaterialInstance = MaterialInstance.GetMaterialInstance(this, materialIndex);
        }

        Texture2D m_CachedTexture;
        // Update is called once per frame
        void Update()
        {
            if (m_CachedTexture != texture) {
                m_CachedTexture = texture;
                var propertyBlock = m_MaterialInstance.sharedPropertyBlock;
                propertyBlock.SetTexture(key, texture);
                m_MaterialInstance.ApplyProperties(propertyBlock);
            }
        }
    }
}