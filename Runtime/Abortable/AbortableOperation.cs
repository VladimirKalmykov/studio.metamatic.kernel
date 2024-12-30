using System;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    /// <summary>
    /// Async operation that can be async disposed or aborted. It means that the operation can be disposed asynchronously and synchronously even it is already disposed asynchronously.
    /// It can handle tree scenarios:
    /// 1. The async operation is disposed synchronously (aborted)
    /// 2. The async operation is disposed asynchronously without being aborted
    /// 3. The async operation is disposed asynchronously but during the async disposing process, it is aborted
    /// </summary>
    public readonly struct AbortableOperation: IPolyDisposable, IAbortable
    {
        public delegate UniTask AsyncAction(AsyncCancellation cancellation);
        public static AbortableOperation Call(AsyncAction action)
        {
            return new AbortableOperation(action);
        }
        
        private readonly AsyncCancellationSource m_CancellationSource;
        private readonly UniTask m_OperationTask;
        public AbortableOperation(AsyncAction action)
        {
            var cancellationSource = AsyncCancellationSource.Create();

            m_CancellationSource = cancellationSource;
            m_OperationTask = action(cancellationSource.Cancellation);
        }

        public async UniTask DisposeAsync()
        {
            m_CancellationSource.TryCancelAsync();
            try
            {
                await m_OperationTask;
            }
            catch (OperationCanceledException)
            {
                // Ignore
            }
            finally
            {
                m_CancellationSource.Dispose();
            }
        }

        public void Dispose()
        {
            m_CancellationSource.TryCancelSync();
        }

        public bool isDisposedAsync => m_CancellationSource.Cancellation.isCancelledAsync;
        public bool isAborted => m_CancellationSource.isAborted;
        public void Abort()
        {
            m_CancellationSource.TryCancelSync();
        }

        public DisposableAction AddAbortedListenerOnce(Action action)
        {
            return m_CancellationSource.AddAbortedListenerOnce(action);
        }
    }
}