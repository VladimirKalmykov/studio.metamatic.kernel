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
    public readonly struct AbortableAsyncOperation: IAsyncDisposableAbortable
    {
        public delegate UniTask AsyncAction(AsyncCancellation cancellation);
        public static AbortableAsyncOperation Call(AsyncAction action)
        {
            return new AbortableAsyncOperation(action);
        }
        // ReSharper disable once InconsistentNaming
        public static AbortableAsyncOperation Aborted => new AbortableAsyncOperation(null);
        
        private readonly AsyncCancellationSource m_CancellationSource;
        private readonly UniTask m_OperationTask;
        public AbortableAsyncOperation(AsyncAction action)
        {
            if (action == null)
            {
                m_CancellationSource = AsyncCancellationSource.Aborted;
                m_OperationTask = UniTask.CompletedTask;
                return;
            }
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

        public bool isDisposedAsync => m_CancellationSource.Cancellation.isCancelledAsync;
        public bool isAborted => m_CancellationSource.isAborted;
        public void Abort()
        {
            m_CancellationSource.TryCancelSync();
        }
        
        /// <summary>
        /// Subscribe to the disposing event. The listener will be called once when the async operation is disposed asynchronously or synchronously.
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public DisposableAction AddDisposeListenerOnce(Action<bool> action)
        {
            return m_CancellationSource.AddDisposedListenerOnce(action);
        }

        public DisposableAction AddAbortedListenerOnce(Action action)
        {
            return m_CancellationSource.AddAbortedListenerOnce(action);
        }
    }
}