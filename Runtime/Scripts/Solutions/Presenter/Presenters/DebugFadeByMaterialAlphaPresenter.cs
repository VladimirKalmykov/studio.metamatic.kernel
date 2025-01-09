using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic.Presenters
{
    public class DebugFadeByMaterialAlphaPresenter : DebugPresenter
    {
        [SerializeField] public float fadeInDuration = 0.5f;
        [SerializeField] public float fadeOutDuration = 0.5f;
        private ComponentMaterialFadeByAlphaHelper m_FadeInOutByAlphaHelper;

        private void Awake()
        {
            m_FadeInOutByAlphaHelper = this.GetOrAddComponent<ComponentMaterialFadeByAlphaHelper>();
        }

        protected override UniTaskVoid OnPresent(Signal cancellationSignal)
        {
            cancellationSignal.ThrowIfRaised();
            return new UniTaskVoid();
        }

        protected override UniTask OnDismiss(Signal cancellationSignal)
        {
            cancellationSignal.ThrowIfRaised();
            return UniTask.CompletedTask;
        }

        protected override void OnReset()
        {
            m_FadeInOutByAlphaHelper.alpha = 0f;
        }
    }
}