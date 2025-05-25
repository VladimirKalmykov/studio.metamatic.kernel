using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic.Presenters
{
    public class FadeByMaterialAlphaPresenter : Presenter
    {
        [SerializeField] public float fadeInDuration = 0.5f;
        [SerializeField] public float fadeOutDuration = 0.5f;
        private SharedMaterialPropertyBlock m_SharedMaterialPropertyBlock;
        private float m_CurrentAlpha;
        private float m_NextAlpha;
        private float m_TransitionStartTime;
        private float m_TransitionDuration;
        private static readonly int Alpha = Shader.PropertyToID("_Alpha");
        private static readonly int NextAlpha = Shader.PropertyToID("_NextAlpha");
        private static readonly int AlphaTransitionDuration = Shader.PropertyToID("_AlphaTransitionDuration");
        private static readonly int AlphaTransitionStartTime = Shader.PropertyToID("_AlphaTransitionStartTime");

        protected override void Awake()
        {
            base.Awake();
            m_SharedMaterialPropertyBlock = this.GetOrAddComponent<SharedMaterialPropertyBlock>();
        }

        protected override UniTaskVoid OnPresent(Signal cancellationSignal)
        {
            PerformShaderBasedAlphaTransition(1f);
            return new UniTaskVoid();
        }

        protected override UniTask OnDismiss(Signal cancellationSignal)
        {
            PerformShaderBasedAlphaTransition(0f);
            return PlayerLoop.Delay(m_TransitionDuration, signal: cancellationSignal);
        }

        private void PerformShaderBasedAlphaTransition(float nextAlpha)
        {
            m_CurrentAlpha = Mathf.Lerp(m_CurrentAlpha, m_NextAlpha, Mathf.Clamp01((Time.time - m_TransitionStartTime) / m_TransitionDuration));
            m_TransitionDuration = m_NextAlpha > m_CurrentAlpha ? fadeInDuration : fadeOutDuration;
            var startTime = Time.time;
            var endTime = startTime + fadeInDuration;
            m_TransitionStartTime = Time.time;
            m_NextAlpha = nextAlpha;
            
            var materialPropertyBlock = m_SharedMaterialPropertyBlock.Value;
            materialPropertyBlock.SetFloat(Alpha, m_CurrentAlpha);
            materialPropertyBlock.SetFloat(NextAlpha, m_NextAlpha);
            materialPropertyBlock.SetFloat(AlphaTransitionDuration, m_TransitionDuration);
            materialPropertyBlock.SetFloat(AlphaTransitionStartTime, m_TransitionStartTime);
            
            m_SharedMaterialPropertyBlock.Apply();
        }

        protected override void OnReset()
        {
            m_CurrentAlpha = 0f;
            m_NextAlpha = 0f;
            var materialPropertyBlock = m_SharedMaterialPropertyBlock.Value;
            materialPropertyBlock.SetFloat(Alpha, m_CurrentAlpha);
            materialPropertyBlock.SetFloat(NextAlpha, m_NextAlpha);
            m_SharedMaterialPropertyBlock.Apply();
        }
    }
}