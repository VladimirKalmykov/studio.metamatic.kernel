using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Metamatic.Effects
{
    public abstract class Effect : IRunnable, INotifyCompletion
    {
        private TaskCompletionSource<float> m_ExecuteTaskCompletionSource;
        private CancellationTokenSource m_CancellationTokenSource;
        
        public Task executionTask
        {
            get
            {
                return m_ExecuteTaskCompletionSource?.Task;
            }
        }

        public Effect()
        {
            m_ExecuteTaskCompletionSource = new TaskCompletionSource<float>();
        }

        public async Task Run(CancellationToken token)
        {
            if (m_CancellationTokenSource != null)
            {
                throw new InvalidOperationException("You can not run effect twice");
            }
            m_DeactivationRequestTaskCompletionSource = new TaskCompletionSource<bool>();
            m_CancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
            var startTime = Time.unscaledTime;
            await RunEffect(m_CancellationTokenSource.Token);
            m_ExecuteTaskCompletionSource.SetResult(Time.unscaledTime - startTime);
            m_IsCompleted = true;
            OnComplete?.Invoke();
        }

        protected abstract Task RunEffect(CancellationToken token);
        
        private TaskCompletionSource<bool> m_DeactivationRequestTaskCompletionSource;

        protected Task disposeRequest
        {
            get
            {
                return m_DeactivationRequestTaskCompletionSource.Task;
            }
        }
        public async void Dispose()
        {
            m_CancellationTokenSource.Cancel();
            m_DeactivationRequestTaskCompletionSource.SetResult(true);
        }

        #region Notify completion

        private bool m_IsCompleted = false;
        private event Action OnComplete;
        public void OnCompleted(Action continuation)
        {
            if (m_IsCompleted)
            {
                continuation();
                return;
            }
            OnComplete += continuation;
        }
        public TaskAwaiter GetAwaiter()
        {
            return executionTask.GetAwaiter();
        }
        #endregion
    }
}