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

        public DisposableAsyncOperation(DisposableProcedure procedure, CancellationToken cancellationToken = default)
        {
            m_CancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            RunProcedure(procedure, cancellationToken);
        }
        
        public async void RunProcedure(DisposableProcedure procedure, CancellationToken cancellationToken)
        {
            try
            {
                Result = await procedure(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // ignored
            }
            catch (Exception e)
            {
                Exception = e;
            }

        }

        public void Dispose()
        {
            m_CancellationTokenSource.Cancel();
        }
    }
}