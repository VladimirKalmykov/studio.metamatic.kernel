using System.Runtime.CompilerServices;
using Sirenix.OdinInspector;

namespace Metamatic
{
    using UnityEngine;

    public class SharedMaterialPropertyBlock : MonoBehaviour
    {
        private (bool has, MaterialPropertyBlock materialPropertyBlock) m_Value;

        // ReSharper disable once ConvertToAutoPropertyWithPrivateSetter
        // ReSharper disable once InconsistentNaming
        public MaterialPropertyBlock Value
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (!m_Value.has)
                {
                    m_Value = (true, new MaterialPropertyBlock());
                    //Renderer.GetPropertyBlock(m_Value);
                }

                return m_Value.materialPropertyBlock;
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
        [Button, HideInEditorMode]
        public void Apply()
        {
            Renderer.SetPropertyBlock(Value);
        }

        public void GetPropertyBlock()
        {
            Renderer.GetPropertyBlock(Value);
        }
        
        [Button, HideInEditorMode]
        public void Fetch()
        {
            Renderer.GetPropertyBlock(Value);
        }
        
        public void SetColor(int propertyId, Color color)
        {
            Value.SetColor(propertyId, color);
        }
        
        public void SetFloat(int propertyId, float value)
        {
            Value.SetFloat(propertyId, value);
        }
        
        public void SetVector(int propertyId, Vector4 vector)
        {
            Value.SetVector(propertyId, vector);
        }
        
        public void SetTexture(int propertyId, Texture texture)
        {
            Value.SetTexture(propertyId, texture);
        }
        
        public void SetMatrix(int propertyId, Matrix4x4 matrix)
        {
            Value.SetMatrix(propertyId, matrix);
        }
    }

}