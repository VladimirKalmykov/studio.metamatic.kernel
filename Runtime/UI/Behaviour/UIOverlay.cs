using System.Threading;
using Cysharp.Threading.Tasks;
using Metamatic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Metamatic.UI
{
    /// <summary>
    /// Allows you to control overlay with backdrop. It is useful for modal dialogs. 
    /// </summary>
    /// <remarks>Use UIOverlay.Instance to get current Overlay. Call Instance.Asquire() to make overlay's backdrop visible. Call Instance.Release() to make it invisible.</remarks>
    [RequireComponent(typeof(CanvasGroup))]
    [RequireComponent(typeof(GraphicRaycaster))]
    public class UIOverlay: UIBehaviour
    {
        private static ObservableWritableValue<UIOverlay> _Instance = new();
        public static ObservableValue<UIOverlay> Instance => _Instance;
        [ShowInInspector, LabelText("Acquire count")]
        private int m_AcquireCount = 0;
        private CancellationTokenSource m_ReleaseCancellationTokenSource;
        private CancellationTokenSource m_RequireCancellationTokenSource;
        private CanvasGroup m_CanvasGroup;
        public CanvasGroup canvasGroup => m_CanvasGroup != null ? m_CanvasGroup : m_CanvasGroup = GetComponent<CanvasGroup>();
        private GraphicRaycaster m_GraphicRaycaster;
        public GraphicRaycaster graphicRaycaster => m_GraphicRaycaster != null ? m_GraphicRaycaster : m_GraphicRaycaster = GetComponent<GraphicRaycaster>();

        private void Awake()
        {
            if (_Instance.value != null)
            {
                throw new System.InvalidOperationException("Only one instance of Overlay is allowed");
            }
            _Instance.value = this;
            canvasGroup.alpha = 0;
            graphicRaycaster.enabled = false;
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            m_RequireCancellationTokenSource?.Cancel();
            m_ReleaseCancellationTokenSource?.Cancel();
            if (_Instance.value == this)
            {
                _Instance.value = null;
            }
        }
        
        [Button]
        public async void Acquire()
        {
            m_ReleaseCancellationTokenSource?.Cancel();
            m_AcquireCount++;
            graphicRaycaster.enabled = true;
            gameObject.SetActive(true);
            if (m_AcquireCount > 1)
            {
                return;
            }
            m_RequireCancellationTokenSource = new CancellationTokenSource();
            await OnRequireAsync(m_RequireCancellationTokenSource.Token);
            if (m_RequireCancellationTokenSource.IsCancellationRequested)
            {
                return;
            }
            canvasGroup.alpha = 1;
        }
        
        [Button]
        public async void Release()
        {
            m_AcquireCount--;
            if (m_AcquireCount < 0)
            {
                m_AcquireCount = 0;
            }
            if (m_AcquireCount == 0)
            {
                m_RequireCancellationTokenSource?.Cancel();
                m_ReleaseCancellationTokenSource = new CancellationTokenSource();
                await OnReleaseAsync(m_ReleaseCancellationTokenSource.Token);
                if (m_ReleaseCancellationTokenSource.IsCancellationRequested)
                {
                    return;
                }
                canvasGroup.alpha = 0;
                graphicRaycaster.enabled = false;
                gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Called when overlay is required. It should return when show animation is completed.
        /// </summary>
        /// <returns></returns>
        protected virtual UniTask OnRequireAsync(CancellationToken cancellationToken = default)
        {
            var startAlpha = canvasGroup.alpha;
            var startTime = Time.time;
            var duration = 0.5f;
            return UniTask.WaitUntil(() =>
            {
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 1, (Time.time - startTime) / duration);
                return canvasGroup.alpha >= 1;
            }, PlayerLoopTiming.Update, cancellationToken: cancellationToken).SuppressCancellationThrow();
        }

        /// <summary>
        /// Called when overlay is released. It should return when hide animation is completed.
        /// </summary>
        /// <returns></returns>
        protected virtual UniTask OnReleaseAsync(CancellationToken cancellationToken = default)
        {
            var startAlpha = canvasGroup.alpha;
            var startTime = Time.time;
            var duration = 0.5f;
            return UniTask.WaitUntil(() =>
            {
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0, (Time.time - startTime) / duration);
                return canvasGroup.alpha == 0;
            }, PlayerLoopTiming.Update, cancellationToken: cancellationToken).SuppressCancellationThrow();
        }
    }
}