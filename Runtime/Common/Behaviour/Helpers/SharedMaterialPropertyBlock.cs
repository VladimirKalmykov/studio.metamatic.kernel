using Sirenix.OdinInspector;

namespace Metamatic
{
    using UnityEngine;

    public class SharedMaterialPropertyBlock : MonoBehaviour
    {
        private MaterialPropertyBlock m_Value;

        // ReSharper disable once ConvertToAutoPropertyWithPrivateSetter
        // ReSharper disable once InconsistentNaming
        public MaterialPropertyBlock Value
        {
            get
            {
                if (m_Value == null)
                {
                    m_Value = new MaterialPropertyBlock();
                    //Renderer.GetPropertyBlock(m_Value);
                }

                return m_Value;
            }
        }

        private Renderer m_Renderer;
        // ReSharper disable once InconsistentNaming
        public Renderer Renderer
        {
            get
            {
                if (m_Renderer == null)
                {
                    m_Renderer = GetComponent<Renderer>();
                }
                return m_Renderer;
            }
        }
        
        
        /// <summary>
        /// Apply the property block to the renderer
        /// </summary>
        [Button]
        public void Apply()
        {
            Renderer.SetPropertyBlock(Value);
        }

        [Button]
        public void GetPropertyBlock()
        {
            Renderer.GetPropertyBlock(Value);
        }
    }

}