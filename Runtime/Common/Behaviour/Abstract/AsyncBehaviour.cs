using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic
{
    public abstract class AsyncBehaviour: MonoBehaviour
    {
        private CancellationTokenSource m_DisableCancellationTokenSource;
        private bool m_IsDisabled = false;
        public delegate UniTask RunAsyncHandler(CancellationToken cancellationToken);
        public CancellationToken componentDisableCancellationToken => m_DisableCancellationTokenSource.Token;

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

        protected abstract void RunAsync(CancellationToken cancellationToken);

        public UniTask RunAsync(RunAsyncHandler handler)
        {
            return handler(componentDisableToken);
        }
    }
}