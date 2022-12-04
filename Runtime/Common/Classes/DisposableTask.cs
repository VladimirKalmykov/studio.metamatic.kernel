using System;
using System.Threading;

namespace Metamatic
{
    public class DisposableTask: IDisposable
    {
        public delegate void DisposableProcedure(CancellationToken cancellationToken);

        private CancellationTokenSource m_CancellationTokenSource;

        public DisposableTask(DisposableProcedure procedure, CancellationToken cancellationToken = default)
        {
            m_CancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            procedure(m_CancellationTokenSource.Token);
        }

        public void Dispose()
        {
            m_CancellationTokenSource.Cancel();
        }
    }
}