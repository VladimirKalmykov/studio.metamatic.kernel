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
        public UniTask componentDisableTask => m_DisableTaskCompletionSource.Task;
        public CancellationToken componentDisableToken => m_DisableCancellationTokenSource.Token;
        protected virtual void OnEnable()
        {
            m_DisableCancellationTokenSource = new CancellationTokenSource();
            m_DisableTaskCompletionSource = new UniTaskCompletionSource();
            RunAsync(componentDisableToken);
        }

        protected virtual void OnDisable()
        {
            m_DisableTaskCompletionSource?.TrySetResult();
            m_DisableCancellationTokenSource?.Cancel();
        }

        protected virtual void RunAsync(CancellationToken cancellationToken)
        {
            
        }
    }
}