using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Metamatic
{
    /// <summary>
    /// C# Promise implementation
    /// Author: Vladimir Kalmykov
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Promise<T>
    {
        private readonly TaskCompletionSource<T> m_TaskCompletionSource;

        public Task<T> task
        {
            get
            {
                return m_TaskCompletionSource.Task;
            }
        }

        public Task<T> Task
        {
            get
            {
                return m_TaskCompletionSource.Task;
            }
        }
        
        public bool isFinalised { get; private set; } = false;

        public delegate void InitializeHandler(Action<T> resolve, Action<Exception> reject, Promise<T> self);

        public event Action OnFinally;
        public event Action OnCancel;
        private CancellationTokenRegistration cancellationTokenRegistration;
        private CancellationTokenSource m_InternalExecutionCancellationTokenSource;

        public CancellationToken executionCancellationToken
        {
            get
            {
                return m_InternalExecutionCancellationTokenSource.Token;
            }
        }
        public Promise(InitializeHandler initialize, CancellationToken cancellationToken = default)
        {
            cancellationTokenRegistration = cancellationToken.Register(Cancel);
            m_InternalExecutionCancellationTokenSource = new CancellationTokenSource();
            
            this.m_TaskCompletionSource = new TaskCompletionSource<T>();
            initialize(Complete, SetError, this);
        }

        public void Cancel()
        {
            OnCancel?.Invoke();
            m_TaskCompletionSource.TrySetCanceled();
            Finallize();
        }

        private void Complete(T result = default)
        {
            if (!m_TaskCompletionSource.TrySetResult(result))
            {
                Debug.LogWarning($"An attempt was made to transition a task to a final state when it had already completed with result {result}");
            }
            Finallize();
        }

        private void SetError(Exception exception)
        {
            m_TaskCompletionSource.SetException(exception);
            Finallize();
        }

        private void Finallize()
        {
            m_InternalExecutionCancellationTokenSource.Cancel();
            cancellationTokenRegistration.Dispose();
            isFinalised = true;
            OnFinally?.Invoke();
        }
    }

    public class Promise : Promise<object>
    {
        public Promise(InitializeHandler initializer, CancellationToken cancellationToken = default): base(initializer, cancellationToken) {}
    }
}