using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic.Presenters
{
    public class FadeByMaterialAlphaPresenter : Presenter
    {
        [SerializeField] public float fadeInDuration = 0.5f;
        [SerializeField] public float fadeOutDuration = 0.5f;
        private ComponentMaterialFadeByAlphaHelper m_FadeInOutByAlphaHelper;

        private void Awake()
        {
            m_FadeInOutByAlphaHelper = this.GetOrAddComponent<ComponentMaterialFadeByAlphaHelper>();
        }

        protected override async UniTaskVoid OnPresent(Signal cancellationSignal)
        {
            Console.Debug("Presenter", "OnEnable Transition Start");
            var startTime = Time.time;
            var endTime = startTime + fadeInDuration;
            await UniTask.WaitUntil(() =>
            {
                if (cancellationSignal.IsCancellationRequested)
                {
                    return true;
                }

                var ratio = Mathf.InverseLerp(startTime, endTime, Time.time);
                if (MiscUtils.Near(ratio, 1))
                {
                    return true;
                }

                var nextAlpha = Mathf.Lerp(m_FadeInOutByAlphaHelper.alpha, 1, ratio);
                m_FadeInOutByAlphaHelper.alpha = nextAlpha;
                return false;
            });
            if (!cancellationSignal.IsCancellationRequested)
            {
                m_FadeInOutByAlphaHelper.alpha = 1f;
            }

            Console.Debug("Presenter", "OnEnable Transition End");
        }

        protected override async UniTask OnDismiss(Signal cancellationSignal)
        {
            Console.Debug("Presenter", "OnDisable Transition Start");
            var startTime = Time.time;
            var endTime = startTime + fadeOutDuration;
            await PlayerLoop.EachFrameUntil(() =>
            {
                var ratio = Mathf.InverseLerp(startTime, endTime, Time.time);
                var nextAlpha = Mathf.Lerp(m_FadeInOutByAlphaHelper.alpha, 0, ratio);
                m_FadeInOutByAlphaHelper.alpha = nextAlpha;
                return MiscUtils.Near(ratio, 1f);
            }, PlayerLoopTiming.Update, cancellationSignal);
            if (!cancellationSignal.IsCancellationRequested)
            {
                m_FadeInOutByAlphaHelper.alpha = 0f;
            }

            Console.Debug("Presenter", "OnDisable Transition End");
        }

        protected override void OnReset()
        {
            m_FadeInOutByAlphaHelper.alpha = 0f;
        }
    }
}