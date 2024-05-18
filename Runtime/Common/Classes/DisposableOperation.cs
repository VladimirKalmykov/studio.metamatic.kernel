using System;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    public class DisposableOperation<T>: IDisposable
    {
        public delegate void DisposableProcedure(Action<T> resolve, Action<Exception> reject, CancellationToken cancellationToken);

        private CancellationTokenSource m_CancellationTokenSource;
        private bool m_Resolved = false;
        private bool m_Rejected = false;
        private  CancellationTokenRegistration cancellationTokenRegistration;
        public T Result { get; private set; }
        public Exception Exception { get; private set; }
        public event Action<Exception> OnError;
        public event Action<T> OnResult;
        public event Action OnCancel;
        public event Action OnDispose;
        private UniTaskCompletionSource<T> m_TaskCompletionSource = new UniTaskCompletionSource<T>();
        public UniTask<T> Task => m_TaskCompletionSource.Task;

        public DisposableOperation(DisposableProcedure procedure, CancellationToken cancellationToken = default)
        {
            m_CancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cancellationTokenRegistration = m_CancellationTokenSource.Token.Register(DoCancel);
            procedure(Resolve, Reject, m_CancellationTokenSource.Token);
        }

        private void DoCancel()
        {
            m_TaskCompletionSource.TrySetCanceled();
            cancellationTokenRegistration.Dispose();
            OnCancel?.Invoke();
        }

        private void Resolve(T value)
        {
            if (m_Resolved)
            {
                throw new Exception("Async operation already resolved");
            }
            m_TaskCompletionSource.TrySetResult(value);
            Result = value;
            OnResult?.Invoke(value);
            m_Resolved = true;
        }
        
        private void Reject(Exception e)
        {
            if (m_Resolved)
            {
                throw new Exception("Async operation already resolved");
            }
            Exception = e;
            m_TaskCompletionSource.TrySetException(e);
            OnError?.Invoke(e);
            m_Rejected = true;
            m_Resolved = true;
        }
        
        public DisposableOperation<T> Then(Action<T> action)
        {
            if (m_Resolved)
            {
                action(Result);
            }
            else
            {
                OnResult += action;
            }
            return this;
        }
        
        public DisposableOperation<T> Catch(Action<Exception> action)
        {
            if (m_Resolved)
            {
                if (m_Rejected)
                {
                    action(Exception);
                }
            }
            else
            {
                OnError += action;
            }
            return this;
        }

        public void Dispose()
        {
            cancellationTokenRegistration.Dispose();
            m_CancellationTokenSource.Cancel();
            OnDispose?.Invoke();
        }
    }
    
    
    public class DisposableAsyncOperationT<T>: IDisposable, IAsyncDisposable
    {
        public delegate UniTask<T> DisposableProcedure(CancellationToken cancellationToken);

        private CancellationTokenSource m_CancellationTokenSource;
        private UniTaskCompletionSource<T> m_UniTaskCompletionSource = new UniTaskCompletionSource<T>();
        private bool m_Resolved = false;
        private bool m_Rejected = false;
        public UniTask<T> Task => m_UniTaskCompletionSource.Task;
        public T Result { get; private set; }
        public Exception Exception { get; private set; }
        public event Action<Exception> OnError;
        public event Action<T> OnResult;
        public event Action OnDispose;

        public DisposableAsyncOperationT(DisposableProcedure procedure, CancellationToken cancellationToken = default)
        {
            m_CancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            RunProcedure(procedure, m_CancellationTokenSource.Token);
        }
        
        public async void RunProcedure(DisposableProcedure procedure, CancellationToken cancellationToken)
        {
            try
            {
                Result = await procedure(cancellationToken);
                m_UniTaskCompletionSource.TrySetResult(Result);
                OnResult?.Invoke(Result);
            }
            catch (OperationCanceledException)
            {
                m_UniTaskCompletionSource.TrySetCanceled();
                Dispose();
            }
            catch (Exception e)
            {
                m_UniTaskCompletionSource.TrySetException(e);
                OnError?.Invoke(e);
                Exception = e;
                m_Rejected = true;
            }
            finally
            {
                m_Resolved = true;
            }

        }
        
        public DisposableAsyncOperationT<T> Then(Action<T> action)
        {
            if (m_Resolved)
            {
                action(Result);
            }
            else
            {
                OnResult += action;
            }
            return this;
        }
        
        public DisposableAsyncOperationT<T> Catch(Action<Exception> action)
        {
            if (m_Resolved)
            {
                if (m_Rejected)
                {
                    action(Exception);
                }
            }
            else
            {
                OnError += action;
            }
            return this;
        }

        public void Dispose()
        {
            m_CancellationTokenSource.Cancel();
            OnDispose?.Invoke();
        }

        public ValueTask DisposeAsync()
        {
            m_CancellationTokenSource.Cancel();
            OnDispose?.Invoke();
            return new ValueTask();
        }
    }
    
    public class DisposableAsyncOperation: IDisposable
    {
        public delegate UniTask DisposableProcedure(CancellationToken cancellationToken);

        private CancellationTokenSource m_CancellationTokenSource;
        private UniTaskCompletionSource m_UniTaskCompletionSource = new UniTaskCompletionSource();
        private bool m_Resolved = false;
        private bool m_Rejected = false;
        public UniTask task => m_UniTaskCompletionSource.Task;
        public Exception Exception { get; private set; }
        public event Action<Exception> OnError;
        public event Action OnDispose;

        public DisposableAsyncOperation(DisposableProcedure procedure, CancellationToken cancellationToken = default)
        {
            m_CancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            RunProcedure(procedure, m_CancellationTokenSource.Token);
        }
        
        public async void RunProcedure(DisposableProcedure procedure, CancellationToken cancellationToken)
        {
            try
            {
                await procedure(cancellationToken);
                m_UniTaskCompletionSource.TrySetResult();
            }
            catch (OperationCanceledException)
            {
                m_UniTaskCompletionSource.TrySetCanceled();
                Dispose();
            }
            catch (Exception e)
            {
                m_UniTaskCompletionSource.TrySetException(e);
                OnError?.Invoke(e);
                Exception = e;
                m_Rejected = true;
            }
            finally
            {
                m_Resolved = true;
            }

        }

        public void Dispose()
        {
            m_CancellationTokenSource.Cancel();
            OnDispose?.Invoke();
        }
    }
}