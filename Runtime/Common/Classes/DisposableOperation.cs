using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    public class DisposableOperation: IDisposable
    {
        public delegate void DisposableProcedure(CancellationToken cancellationToken);

        private CancellationTokenSource m_CancellationTokenSource;

        public DisposableOperation(DisposableProcedure procedure, CancellationToken cancellationToken = default)
        {
            m_CancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            procedure(m_CancellationTokenSource.Token);
        }

        public void Dispose()
        {
            m_CancellationTokenSource.Cancel();
        }
    }
    
    
    public class DisposableAsyncOperation<T>: IDisposable
    {
        public delegate UniTask<T> DisposableProcedure(CancellationToken cancellationToken);

        private CancellationTokenSource m_CancellationTokenSource;
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
                OnResult?.Invoke(Result);
            }
            catch (OperationCanceledException)
            {
                Dispose();
            }
            catch (Exception e)
            {
                OnError?.Invoke(e);
                Exception = e;
            }

        }

        public void Dispose()
        {
            m_CancellationTokenSource.Cancel();
            OnDispose?.Invoke();
        }
    }
}