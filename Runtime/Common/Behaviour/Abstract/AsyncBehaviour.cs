using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic
{
    public abstract class AsyncBehaviour: AsyncBehaviourBase
    {
        protected bool AwakeIsReady;
        protected bool IsReady;
        private event Action OnAwakeReady;
        private event Action OnReady;

        private CancellationTokenAwaitable InternalSetReady()
        {
            IsReady = true;
            OnReady?.Invoke();
            return componentDisableCancellationToken.WaitUntilCanceled();
        }
        private CancellationTokenAwaitable InternalAwakeSetReady()
        {
            AwakeIsReady = true;
            OnAwakeReady?.Invoke();
            return componentDestroyToken.WaitUntilCanceled();
        }
        
        /// <summary>
        /// Internal method to wait for Awake method to be ready. Use it only if AsyncAwake method is overridden
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private UniTask WaitForAwakeReady(CancellationToken cancellationToken)
        {
            var tcs = new UniTaskCompletionSource();

            void AwakeReadyHandler()
            {
                tcs.TrySetResult();
            }
            OnAwakeReady += AwakeReadyHandler;
            cancellationToken.Register(() =>
            {
                tcs.TrySetCanceled();
                OnAwakeReady -= AwakeReadyHandler;
            });
            return tcs.Task;
        }
        public async UniTask WhenReady(CancellationToken cancellationToken = default)
        {
            if (IsReady)
            {
                return;
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

            var timeoutCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var timeoutTask = UniTask.Delay(TimeSpan.FromSeconds(30), DelayType.DeltaTime, PlayerLoopTiming.TimeUpdate,
                cancellationToken);
            var result = await UniTask.WhenAny(timeoutTask, tcs.Task);
            if (result == 0)
            {
                throw new TimeoutException($"Async behaviour {this.GetType().Name} initialization timeout");
            }
            timeoutCancellationTokenSource.Cancel();
            timeoutCancellationTokenSource.Dispose();
        }
        
        // ReSharper disable once MemberCanBePrivate.Global
        public DisposableAction SubscribeReady(Action action)
        {
            if (IsReady)
            {
                action();
                return DisposableAction.Empty;
            }
            OnReady += action;
            return new DisposableAction(() => OnReady -= action);
        }
        protected sealed override async UniTaskVoid RunAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (!AwakeIsReady)
                {
                    await WaitForAwakeReady(cancellationToken);
                }
                await Run(InternalSetReady, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                var type = this.GetType();
            }
            catch (Exception e)
            {
                var type = this.GetType();
                #if METAMATIC_TERMINAL
                Terminal.LogError($"AsyncBehaviour {type} failed to run: {e.Message}");
                #endif
                Debug.LogError($"Async behaviour {type} failed to run: {e}");
            }
        }

        protected sealed override async UniTaskVoid RunUntilDestroy(CancellationToken cancellationToken)
        {
            try
            {
                await AsyncAwake(InternalAwakeSetReady, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                var type = this.GetType();
                Debug.Log($"Async behaviour async awake {type} was canceled.");
            }
            catch (Exception e)
            {
                var type = this.GetType();
#if METAMATIC_TERMINAL
                Terminal.LogError($"AsyncBehaviour {type} failed to run: {e.Message}");
#endif
                Debug.LogError($"Async behaviour async awake {type} failed to run: {e}");
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
        
        protected delegate CancellationTokenAwaitable Ready();
        private bool m_InitializedOnDemand;

        protected abstract UniTask Run(Ready ready, CancellationToken cancellationToken);
        
        protected virtual UniTask AsyncAwake(Ready ready, CancellationToken cancellationToken)
        {
            AwakeIsReady = true;
            OnAwakeReady?.Invoke();
            return UniTask.CompletedTask;
        }
    }
}