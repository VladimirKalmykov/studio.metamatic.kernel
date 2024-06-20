using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic
{
    public abstract class AsyncBehaviour: MonoBehaviour
    {
        private CancellationTokenSource m_DisableCancellationTokenSource;
        private bool m_IsDisabled;
        public delegate UniTask RunAsyncHandler(CancellationToken cancellationToken);
        public CancellationToken componentDisableCancellationToken => m_DisableCancellationTokenSource.Token;
        
        private event Action OnValidateEvent;
        private event Action OnDrawGizmosEvent;
        
        protected bool isAboutToDestroy { get; private set; }
        

        public CancellationToken componentDisableToken
        {
            get
            {
                if (m_DisableCancellationTokenSource == null)
                {
                    var disableCancellationToken = m_DisableCancellationTokenSource = new CancellationTokenSource();
                    if (!m_IsDisabled)
                    {
                        Action cancelToken = null;
                        cancelToken = () =>
                        {
                            m_DisableCancellationTokenSource.Cancel();
                            m_DisableCancellationTokenSource = null;
                            OnDisabled -= cancelToken;
                        };
                        OnDisabled += cancelToken;
                    }
                    else
                    {
                        m_DisableCancellationTokenSource.Cancel();
                        m_DisableCancellationTokenSource = null;
                        return disableCancellationToken.Token;
                    }
                }

                return m_DisableCancellationTokenSource.Token;
            }
        }
        public event Action OnDisabled;
        protected virtual void OnEnable()
        {
            m_IsDisabled = false;
            RunAsync(componentDisableToken);
        }

        protected virtual void OnDisable()
        {
            m_IsDisabled = true;
            OnDisabled?.Invoke();
        }

        protected void OnDestroy()
        {
            isAboutToDestroy = true;
        }

        protected abstract void RunAsync(CancellationToken cancellationToken);
        
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
        protected void OnValidate()
        {
            OnValidateEvent?.Invoke();
        }

        protected void OnDrawGizmos()
        {
            OnDrawGizmosEvent?.Invoke();
        }
        #endif
    }
}