namespace Metamatic
{
    using System.Threading;
    using Cysharp.Threading.Tasks;
    using UnityEngine;

    public class SharedMaterialPropertyBlock : AsyncBehaviour
    {
        private MaterialPropertyBlock m_Value;

        // ReSharper disable once ConvertToAutoPropertyWithPrivateSetter
        // ReSharper disable once InconsistentNaming
        public MaterialPropertyBlock Value => m_Value;

        private Renderer m_Renderer;
        // ReSharper disable once InconsistentNaming
        public Renderer Renderer => m_Renderer;

        protected override void Awake()
        {
            m_Value = new MaterialPropertyBlock();
            m_Renderer = GetComponent<Renderer>();
            base.Awake();
        }

        protected override async UniTask Run(Ready ready, CancellationToken cancellationToken)
        {
            m_Renderer.GetPropertyBlock(m_Value);
            await ready();
        }
        
        /// <summary>
        /// Apply the property block to the renderer
        /// </summary>
        public void Apply()
        {
            m_Renderer.SetPropertyBlock(m_Value);
        }
    }

}