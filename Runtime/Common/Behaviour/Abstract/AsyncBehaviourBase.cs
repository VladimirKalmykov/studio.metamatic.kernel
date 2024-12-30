using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic
{
    public abstract class AsyncBehaviourBase: MonoBehaviour, IExposedDisposedSync
    {
        private SignalSource m_DisableCancellationSignalSource;
        private SignalSource m_DestroyCancellationSignalSource;
        private bool m_IsDisabled;
        public delegate UniTask RunAsyncHandler(Signal cancellationToken);
        internal Signal componentDisableCancellationToken => m_DisableCancellationSignalSource.Signal;
        
        private event Action OnValidateEvent;
        private event Action OnDrawGizmosEvent;
        
        protected bool isAboutToDestroy { get; private set; }
        
        public Signal componentDestroySignal => m_DestroyCancellationSignalSource.Signal;

        public Signal componentDisableSignal
        {
            get
            {
                if (m_IsDisabled)
                {
                    return Signal.Raised;
                }

                return m_DisableCancellationSignalSource.Signal;
            }
        }
        public event Action OnDisabled;

        protected virtual void Awake()
        {
            m_DestroyCancellationSignalSource = SignalSource.Create();
            RunUntilDestroy(m_DestroyCancellationSignalSource.Signal).Forget();
        }
        
        protected virtual void OnEnable()
        {
            m_IsDisabled = false;
            m_DisableCancellationSignalSource = SignalSource.Create();
            RunAsync(componentDisableSignal).Forget();
        }

        protected virtual void OnDisable()
        {
            m_IsDisabled = true;
            m_DisableCancellationSignalSource.TryRaise();
            OnDisabled?.Invoke();
        }

        protected virtual void OnDestroy()
        {
            isAboutToDestroy = true;
            m_DestroyCancellationSignalSource.TryRaise();
            OnDisposedSync?.Invoke();
        }

        protected abstract UniTaskVoid RunAsync(Signal signal);
        protected abstract UniTaskVoid RunUntilDestroy(Signal signal);
        
        /// <summary>
        /// IDisposable object will be disposed on component disable
        /// </summary>
        /// <param name="disposable"></param>
        public IDisposable WithinLifecycle(IDisposable disposable)
        {
            if (m_IsDisabled)
            {
                disposable.Dispose();
                return VoidDisposable.None;
            }
            OnDisabled += disposable.Dispose;
            return DisposableAction.Create(() =>
            {
                disposable.Dispose();
                OnDisabled -= disposable.Dispose;
            });
        }
        
        protected DisposableAction SubscribeValidate(Action action)
        {
            OnValidateEvent += action;
            return new DisposableAction(() => OnValidateEvent -= action);
        }
        
        public DisposableAction SubscribeDrawGizmos(Action action)
        {
            OnDrawGizmosEvent += action;
            return new DisposableAction(() => OnDrawGizmosEvent -= action);
        }

        #if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            OnValidateEvent?.Invoke();
        }

        protected virtual void OnDrawGizmos()
        {
            OnDrawGizmosEvent?.Invoke();
        }
        #endif
        public bool isDisposedSync => m_DestroyCancellationSignalSource.IsCancellationRequested;
        public event Action OnDisposedSync;
    }
}