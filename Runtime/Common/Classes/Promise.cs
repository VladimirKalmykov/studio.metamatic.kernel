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
        
        public bool isFinalised { get; private set; }

        public delegate void InitializeHandler(Action<T> resolve, Action<Exception> reject, Promise<T> self);

        public event Action<T> OnResult;
        public event Action<Exception> OnError;
        public event Action OnFinally;
        public event Action OnCancel;
        private bool m_Cancelled;
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
            
            m_TaskCompletionSource = new TaskCompletionSource<T>();
            initialize(Complete, SetError, this);
        }
        
        ~Promise()
        {
            if (!isFinalised)
            {
                Finallize();
            }
        }

        public void Cancel()
        {
            if (m_Cancelled)
            {
                return;
            }
            m_Cancelled = true;
            OnCancel?.Invoke();
            m_TaskCompletionSource.TrySetCanceled();
            Finallize();
        }
        
        public Promise<T> Then(Action<T> onResult)
        {
            OnResult += onResult;
            return this;
        }
        
        public Promise<T> Catch(Action<Exception> onError)
        {
            OnError += onError;
            return this;
        }

        private void Complete(T result = default)
        {
            OnResult?.Invoke(result);
            if (!m_TaskCompletionSource.TrySetResult(result))
            {
                Debug.LogWarning($"An attempt was made to transition a task to a final state when it had already completed with result {result}");
            }
            Finallize();
        }

        private void SetError(Exception exception)
        {
            OnError?.Invoke(exception);
            m_TaskCompletionSource.SetException(exception);
            Finallize();
        }

        private void Finallize()
        {
            m_InternalExecutionCancellationTokenSource.Cancel();
            cancellationTokenRegistration.Dispose();
            isFinalised = true;
            OnFinally?.Invoke();
            GC.SuppressFinalize(this);
        }
    }

    public class Promise : Promise<object>
    {
        public Promise(InitializeHandler initializer, CancellationToken cancellationToken = default): base(initializer, cancellationToken) {}
    }
    
    public class UniPromise
    {
        private readonly TaskCompletionSource<object> m_TaskCompletionSource;

        public Task<object> task
        {
            get
            {
                return m_TaskCompletionSource.Task;
            }
        }

        public Task<object> Task
        {
            get
            {
                return m_TaskCompletionSource.Task;
            }
        }
        
        public bool isFinalised { get; private set; }

        public delegate void InitializeHandler(Action resolve, Action<Exception> reject, UniPromise self);

        public event Action OnResult;
        public event Action<Exception> OnError;
        public event Action OnFinally;
        public event Action OnCancel;
        private bool m_Cancelled;
        private CancellationTokenRegistration cancellationTokenRegistration;
        private CancellationTokenSource m_InternalExecutionCancellationTokenSource;

        public CancellationToken executionCancellationToken
        {
            get
            {
                return m_InternalExecutionCancellationTokenSource.Token;
            }
        }
        public UniPromise(InitializeHandler initialize, CancellationToken cancellationToken = default)
        {
            cancellationTokenRegistration = cancellationToken.Register(Cancel);
            m_InternalExecutionCancellationTokenSource = new CancellationTokenSource();
            
            m_TaskCompletionSource = new TaskCompletionSource<object>();
            initialize(Complete, SetError, this);
        }
        
        ~UniPromise()
        {
            if (!isFinalised)
            {
                Finallize();
            }
        }

        public void Cancel()
        {
            if (m_Cancelled)
            {
                return;
            }
            m_Cancelled = true;
            OnCancel?.Invoke();
            m_TaskCompletionSource.TrySetCanceled();
            Finallize();
        }
        
        public UniPromise Then(Action onResult)
        {
            OnResult += onResult;
            return this;
        }
        
        public UniPromise Catch(Action<Exception> onError)
        {
            OnError += onError;
            return this;
        }

        private void Complete()
        {
            OnResult?.Invoke();
            if (!m_TaskCompletionSource.TrySetResult(null))
            {
                Debug.LogWarning($"An attempt was made to transition a task to a final state when it had already completed");
            }
            Finallize();
        }

        private void SetError(Exception exception)
        {
            OnError?.Invoke(exception);
            m_TaskCompletionSource.SetException(exception);
            Finallize();
        }

        private void Finallize()
        {
            m_InternalExecutionCancellationTokenSource.Cancel();
            cancellationTokenRegistration.Dispose();
            isFinalised = true;
            OnFinally?.Invoke();
            GC.SuppressFinalize(this);
        }
    }
}