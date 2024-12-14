using UnityEngine;

namespace Metamatic
{
    [RequireComponent(typeof(SharedMaterialPropertyBlock))]
    public class MaterialFadeInOutByAlpha: MonoBehaviour
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
            m_SharedMaterialPropertyBlock.Value.SetFloat(Alpha, nextAlpha);
            m_SharedMaterialPropertyBlock.SetPropertyBlock();
        }
    }
}