using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic
{
    public abstract class AsyncBehaviour: MonoBehaviour
    {
        private UniTaskCompletionSource m_DisableTaskCompletionSource;
        private CancellationTokenSource m_DisableCancellationTokenSource;

        public delegate UniTask RunAsyncHandler(CancellationToken cancellationToken);
        public UniTask componentDisableTask => m_DisableTaskCompletionSource.Task;

        public CancellationToken componentDisableToken
        {
            get
            {
                if (m_DisableCancellationTokenSource == null)
                {
                    m_DisableCancellationTokenSource = new CancellationTokenSource();
                    Action cancelToken = null;
                    cancelToken = () =>
                    {
                        m_DisableCancellationTokenSource.Cancel();
                        m_DisableCancellationTokenSource = null;
                        OnDisabled -= cancelToken;
                    };
                    OnDisabled += cancelToken;
                }

                return m_DisableCancellationTokenSource.Token;
            }
        }
        public event Action OnDisabled;
        protected virtual void OnEnable()
        {
            m_DisableTaskCompletionSource = new UniTaskCompletionSource();
            RunAsync(componentDisableToken);
        }

        protected virtual void OnDisable()
        {
            OnDisabled?.Invoke();
            m_DisableTaskCompletionSource?.TrySetResult();
        }

        protected abstract void RunAsync(CancellationToken cancellationToken);

        public UniTask RunAsync(RunAsyncHandler handler)
        {
            return handler(componentDisableToken);
        }
    }
}