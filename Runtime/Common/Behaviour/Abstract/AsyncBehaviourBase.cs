using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic
{
    public abstract class AsyncBehaviourBase: MonoBehaviour
    {
        private CancellationTokenSource m_DisableCancellationTokenSource = new();
        private CancellationTokenSource m_DestroyCancellationTokenSource;
        private bool m_IsDisabled;
        public delegate UniTask RunAsyncHandler(CancellationToken cancellationToken);
        internal CancellationToken componentDisableCancellationToken => m_DisableCancellationTokenSource?.Token ?? CancellationToken.None;
        
        private event Action OnValidateEvent;
        private event Action OnDrawGizmosEvent;
        
        protected bool isAboutToDestroy { get; private set; }
        
        public CancellationToken componentDestroyToken => m_DestroyCancellationTokenSource.Token;

        public CancellationToken componentDisableToken
        {
            get
            {
                if (m_DisableCancellationTokenSource == null)
                {
                    var disableCancellationToken = m_DisableCancellationTokenSource = new CancellationTokenSource();
                    if (m_IsDisabled)
                    {
                        m_DisableCancellationTokenSource.Cancel();
                        m_DisableCancellationTokenSource.Dispose();
                        m_DisableCancellationTokenSource = null;
                        return disableCancellationToken.Token;
                    }
                }

                return m_DisableCancellationTokenSource.Token;
            }
        }
        public event Action OnDisabled;

        protected virtual void Awake()
        {
            m_DestroyCancellationTokenSource = new CancellationTokenSource();
            RunUntilDestroy(m_DestroyCancellationTokenSource.Token).Forget();
        }
        
        protected virtual void OnEnable()
        {
            m_IsDisabled = false;
            RunAsync(componentDisableToken).Forget();
        }

        protected virtual void OnDisable()
        {
            m_IsDisabled = true;
            m_DisableCancellationTokenSource.Cancel();
            m_DisableCancellationTokenSource.Dispose();
            m_DisableCancellationTokenSource = null;
            OnDisabled?.Invoke();
        }

        protected void OnDestroy()
        {
            isAboutToDestroy = true;
            #if DEBUG
            if (m_DestroyCancellationTokenSource == null)
            {
                Debug.Log($"m_DestroyCancellationTokenSource is null in component {name} {GetType().Name}");
                return;
            }
            #endif
            
            m_DestroyCancellationTokenSource.Cancel();
            m_DestroyCancellationTokenSource.Dispose();
        }

        protected abstract UniTaskVoid RunAsync(CancellationToken cancellationToken);
        protected abstract UniTaskVoid RunUntilDestroy(CancellationToken cancellationToken);
        
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
    }
}