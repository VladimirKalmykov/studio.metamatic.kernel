using UnityEngine;

namespace Metamatic
{
    [RequireComponent(typeof(SharedMaterialPropertyBlock))]
    public class ComponentMaterialFadeByAlphaHelper: MonoBehaviour
    {
        private SharedMaterialPropertyBlock m_SharedMaterialPropertyBlock;
        private static readonly int Alpha = Shader.PropertyToID("_Alpha");
        
        private float m_Alpha;

        public float alpha
        {
            get => m_Alpha;
            set
            {
                SetAlpha(value);
            }
        }

        private void Awake()
        {
            m_SharedMaterialPropertyBlock = GetComponent<SharedMaterialPropertyBlock>();
        }
        
        private void SetAlpha(float nextAlpha)
        {
            m_Alpha = nextAlpha;
            if (m_SharedMaterialPropertyBlock != null)
            {
                m_SharedMaterialPropertyBlock.Value.SetFloat(Alpha, nextAlpha);
                m_SharedMaterialPropertyBlock.Apply();
            }
        }

        private void OnDestroy()
        {
            m_SharedMaterialPropertyBlock = null;
        }
    }
}