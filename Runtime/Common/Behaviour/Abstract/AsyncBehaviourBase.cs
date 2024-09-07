using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic
{
    public abstract class AsyncBehaviourBase: MonoBehaviour, IExposedDisposed
    {
        private CancellationSignalSource m_DisableCancellationSignalSource;
        private CancellationSignalSource m_DestroyCancellationSignalSource;
        private bool m_IsDisabled;
        public delegate UniTask RunAsyncHandler(CancellationSignal cancellationToken);
        internal CancellationSignal componentDisableCancellationToken => m_DisableCancellationSignalSource.Signal;
        
        private event Action OnValidateEvent;
        private event Action OnDrawGizmosEvent;
        
        protected bool isAboutToDestroy { get; private set; }
        
        public CancellationSignal componentDestroyToken => m_DestroyCancellationSignalSource.Signal;

        public CancellationSignal componentDisableSignal
        {
            get
            {
                if (m_IsDisabled)
                {
                    return CancellationSignal.Cancelled;
                }

                return m_DisableCancellationSignalSource.Signal;
            }
        }
        public event Action OnDisabled;

        protected virtual void Awake()
        {
            m_DestroyCancellationSignalSource = CancellationSignalSource.Create();
            RunUntilDestroy(m_DestroyCancellationSignalSource.Signal).Forget();
        }
        
        protected virtual void OnEnable()
        {
            m_IsDisabled = false;
            m_DisableCancellationSignalSource = CancellationSignalSource.Create();
            RunAsync(componentDisableSignal).Forget();
        }

        protected virtual void OnDisable()
        {
            m_IsDisabled = true;
            m_DisableCancellationSignalSource.TryCancel();
            OnDisabled?.Invoke();
        }

        protected virtual void OnDestroy()
        {
            isAboutToDestroy = true;
            m_DestroyCancellationSignalSource.TryCancel();
            OnDisposed?.Invoke();
        }

        protected abstract UniTaskVoid RunAsync(CancellationSignal signal);
        protected abstract UniTaskVoid RunUntilDestroy(CancellationSignal signal);
        
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
        
        protected DisposableAction SubscribeDrawGizmos(Action action)
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
        public bool isDisposed => m_DestroyCancellationSignalSource.IsCancellationRequested;
        public event Action OnDisposed;
    }
}