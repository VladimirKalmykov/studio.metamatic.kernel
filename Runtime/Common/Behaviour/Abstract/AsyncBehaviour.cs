using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic
{
    public abstract class AsyncBehaviour : AsyncBehaviourBase
    {
        protected bool AwakeIsReady;
        protected bool IsReady;
        private event Action OnAwakeReady;
        private event Action OnReady;

        private UniTask InternalSetReady()
        {
            IsReady = true;
            OnReady?.Invoke();
            OnBehaviourReady();
            return componentDisableCancellationToken.ToUniTask();
        }

        private UniTask InternalAwakeSetReady()
        {
            AwakeIsReady = true;
            OnAwakeReady?.Invoke();
            return componentDestroySignal.ToUniTask();
        }

        /// <summary>
        /// Internal method to wait for Awake method to be ready. Use it only if AsyncAwake method is overridden
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private UniTask WaitForAwakeReady(CancellationSignal cancellationToken)
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

        public async UniTask WhenReady(CancellationSignal signal = default)
        {
            if (IsReady)
            {
                return;
            }

            var tcs = new UniTaskCompletionSource();
            IDisposable readyListener = null;
            var cancellationTokenRegistration = signal.Register(() =>
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

            var timeoutSignalSource = CancellationSignalSource.CreateLinked(signal);
            var timeoutTask = UniTask.Delay(TimeSpan.FromSeconds(30), DelayType.DeltaTime, PlayerLoopTiming.TimeUpdate,
                signal);
            var result = await UniTask.WhenAny(timeoutTask, tcs.Task);
            try
            {
                if (result == 0)
                {
                    throw new TimeoutException($"Async behaviour {this.GetType().Name} initialization timeout");
                }
            }
            finally
            {
                timeoutSignalSource.TryCancel();
            }
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

        protected sealed override async UniTaskVoid RunAsync(CancellationSignal signal)
        {
            try
            {
                if (!AwakeIsReady)
                {
                    await WaitForAwakeReady(signal);
                }

                await Run(InternalSetReady, signal);
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
                Console.Error("AsyncBehaviour", e);
            }
        }

        protected sealed override async UniTaskVoid RunUntilDestroy(CancellationSignal signal)
        {
            try
            {
                await AsyncAwake(InternalAwakeSetReady, signal);
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

        public static async UniTask<T> Resolve<T>(GameObject gameObject, CancellationSignal signal = default,
            float timeout = 10f)
            where T : Service
        {
            var behaviour = gameObject.GetComponent<T>();
            await behaviour.WhenReady(signal);
            return behaviour;
        }

        public static async UniTask<T> Resolve<T>(MonoBehaviour monoBehaviour,
            CancellationSignal signal = default, float timeout = 10f)
            where T : Service
        {
            var behaviour = monoBehaviour.GetComponent<T>();
            await behaviour.WhenReady(signal);
            return behaviour;
        }

        protected delegate UniTask Ready();

        private bool m_InitializedOnDemand;

        protected abstract UniTask Run(Ready ready, CancellationSignal signal);

        protected virtual UniTask AsyncAwake(Ready ready, CancellationSignal signal)
        {
            AwakeIsReady = true;
            OnAwakeReady?.Invoke();
            return UniTask.CompletedTask;
        }
        
        protected virtual void OnBehaviourReady()
        {
            /* Override this method to implement custom logic */
        }
    }
}