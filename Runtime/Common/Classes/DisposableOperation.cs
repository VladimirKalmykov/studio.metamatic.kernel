using System;
using System.Threading;
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

        public DisposableOperation(DisposableProcedure procedure, CancellationToken cancellationToken = default)
        {
            m_CancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cancellationTokenRegistration = m_CancellationTokenSource.Token.Register(DoCancel);
            procedure(Resolve, Reject, m_CancellationTokenSource.Token);
        }

        private void DoCancel()
        {
            cancellationTokenRegistration.Dispose();
            OnCancel?.Invoke();
        }

        private void Resolve(T value)
        {
            if (m_Resolved)
            {
                throw new Exception("Async operation already resolved");
            }
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
    
    
    public class DisposableAsyncOperation<T>: IDisposable
    {
        public delegate UniTask<T> DisposableProcedure(CancellationToken cancellationToken);

        private CancellationTokenSource m_CancellationTokenSource;
        private UniTaskCompletionSource<T> m_UniTaskCompletionSource = new UniTaskCompletionSource<T>();
        private bool m_Resolved = false;
        private bool m_Rejected = false;
        public UniTask<T> task => m_UniTaskCompletionSource.Task;
        public T Result { get; private set; }
        public Exception Exception { get; private set; }
        public event Action<Exception> OnError;
        public event Action<T> OnResult;
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
        
        public DisposableAsyncOperation<T> Then(Action<T> action)
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
        
        public DisposableAsyncOperation<T> Catch(Action<Exception> action)
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
    }
}