using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic
{
    public abstract class AsyncBehaviour: AsyncBehaviourBase
    {
        private bool m_IsReady;
        private event Action OnReady;

        private async UniTask InternalSetReady()
        {
            m_IsReady = true;
            OnReady?.Invoke();
            await componentDisableCancellationToken;
        }
        public async UniTask WhenReady(CancellationToken cancellationToken = default)
        {
            if (!m_Demanded)
            {
                Demand();
            }
            var tcs = new UniTaskCompletionSource();
            IDisposable readyListener = null;
            var cancellationTokenRegistration = cancellationToken.Register(() =>
            {
                // ReSharper disable once AccessToModifiedClosure
                readyListener?.Dispose();
                tcs.TrySetCanceled();
            });
            
            readyListener = SubscribeReady(() =>
            {
                // ReSharper disable once AccessToModifiedClosure
                readyListener?.Dispose();
                cancellationTokenRegistration.Dispose();
                tcs.TrySetResult();
            });

            var timeoutTask = UniTask.Delay(TimeSpan.FromSeconds(30), DelayType.Realtime, PlayerLoopTiming.TimeUpdate,
                cancellationToken);
            var result = await UniTask.WhenAny(timeoutTask, tcs.Task);
            if (result == 0)
            {
                throw new TimeoutException($"Async behaviour {this.GetType().Name} initialization timeout");
            }
        }
        
        // ReSharper disable once MemberCanBePrivate.Global
        public DisposableAction SubscribeReady(Action action)
        {
            if (m_IsReady)
            {
                action();
                return DisposableAction.Empty;
            }
            OnReady += action;
            return new DisposableAction(() => OnReady -= action);
        }
        
        /// <summary>
        /// Demand service initialization (only if InitializeOnDemand is true)
        /// </summary>
        private void Demand()
        {
            if (!m_Demanded)
            {
                m_Demanded = true;
                m_DemandCompletionSource = m_DemandCompletionSource ?? new UniTaskCompletionSource();
                m_DemandCompletionSource.TrySetResult();
            }
        }
        
        /// <summary>
        /// Demand async behaviour initialization (gives any effect only if Run method is awaited demand request by calling await WhenDemand() method)
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private UniTask Demand(CancellationToken cancellationToken)
        {
            Demand();
            return WhenReady(cancellationToken);
        }

        /// <summary>
        /// Wait for demand
        /// </summary>
        /// <returns></returns>
        public UniTask WhenDemand()
        {
            return m_DemandCompletionSource.Task;
        }

        private bool m_Demanded;
        private UniTaskCompletionSource m_DemandCompletionSource;
        protected sealed override async UniTaskVoid RunAsync(CancellationToken cancellationToken)
        {
            var type = this.GetType();
            cancellationToken.Register(() =>
            {
                m_DemandCompletionSource?.TrySetCanceled();
                m_DemandCompletionSource = null;
            });
            try
            {
                m_DemandCompletionSource = m_DemandCompletionSource ?? new UniTaskCompletionSource();
                await Run(InternalSetReady, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                Debug.Log($"Service {type} was canceled.");
            }
            catch (Exception e)
            {
                #if METAMATIC_TERMINAL
                Terminal.LogError($"AsyncBehaviour {type} failed to run: {e.Message}");
                #endif
                Debug.LogError($"Service {type} failed to run: {e}");
            }
        }

        public static async UniTask<T> Resolve<T>(GameObject gameObject, CancellationToken cancellationToken = default, float timeout = 10f)
            where T : Service
        {
            var behaviour = gameObject.GetComponent<T>();
            await behaviour.WhenReady(cancellationToken);
            return behaviour;
        }
        
        public static async UniTask<T> Resolve<T>(MonoBehaviour monoBehaviour, CancellationToken cancellationToken = default, float timeout = 10f)
            where T : Service
        {
            var behaviour = monoBehaviour.GetComponent<T>();
            await behaviour.WhenReady(cancellationToken);
            return behaviour;
        }
        
        protected delegate UniTask Ready();
        private bool m_InitializedOnDemand;

        protected abstract UniTask Run(Ready ready, CancellationToken cancellationToken);
    }
}