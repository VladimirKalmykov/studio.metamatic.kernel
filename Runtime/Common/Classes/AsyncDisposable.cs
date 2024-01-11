using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    
    interface IAsyncDisposable: IDisposable
    {
        public event Action OnAsyncDisposeCompleted;
        public UniTask DisposeAsync();
    }
    
    /// <summary>
    /// The concept of async disposable is to allow to dispose object asynchronously. For example, you can dispose confirm dialog and it need to wait for animation to complete.
    /// </summary>
    /// <example>
    ///using (var dialog = confirmationDialog.Show("Are you sure you want to exit to main menu?")) {
    ///            var confirmed = await dialog.confirmed;
    ///            dialog.AsyncDispose();
    ///            if (confirmed) {
    ///                 // dialog is async disposable, so we should await for it to complete
    ///                 await dialog.AsyncDisposeCompleted;
    ///                 goToMainMenu();
    ///            }
    ///     }
    /// </example>
    public abstract class AsyncDisposable: IAsyncDisposable
    {
        private bool m_AsyncDisposeRequested = false;
        private CancellationTokenSource m_AsyncDisposeCancellationTokenSource;
        public event Action OnAsyncDisposeCompleted;
        private UniTaskCompletionSource m_AsyncDisposeCompleted = new UniTaskCompletionSource();
        public UniTask asyncDisposeCompleted => m_AsyncDisposeCompleted.Task;
        public async UniTask DisposeAsync()
        {
            if (m_AsyncDisposeRequested)
            {
                throw new InvalidOperationException("Async dispose already requested");
            }
            m_AsyncDisposeRequested = true;
            m_AsyncDisposeCancellationTokenSource = new CancellationTokenSource();
            try
            {
                await OnDisposeAsync(m_AsyncDisposeCancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                // Skip
            }
            finally
            {
                m_AsyncDisposeCompleted.TrySetResult();
                m_AsyncDisposeCancellationTokenSource = null;
                OnAsyncDisposeCompleted?.Invoke();
            }
        }

        public void Dispose()
        {
            if (m_AsyncDisposeCancellationTokenSource != null)
            {
                /* 
                 * If async dispose was requested, we need to cancel it and dispose immediately
                 */
                m_AsyncDisposeCancellationTokenSource.Cancel();
            }
            else
            {
                OnAsyncDisposeCompleted?.Invoke();
            }
            OnDispose();
        }

        protected abstract void OnDispose();
        
        /// <summary>
        /// Async dispose method should handle case when dispose was requested before async dispose was called, e.g. to complete animation.
        /// </summary>
        /// <param name="cancellationToken">Cancel async dispose and force to immediate dispose</param>
        /// <returns></returns>
        protected abstract UniTask OnDisposeAsync(CancellationToken cancellationToken = default);
    }

    public class AsyncDisposableOperation<T>: AsyncDisposable
    {
        private readonly DisposableAsyncOperation<T> m_DisposableAsyncOperation;
        private Func<UniTask> m_OnAsyncDisposeRequest;
        private Action m_OnDispose;
        public DisposableAsyncOperation<T> operation => m_DisposableAsyncOperation;
        public AsyncDisposableOperation(DisposableAsyncOperation<T>.DisposableProcedure asyncOperation, Func<UniTask> onAsyncDisposeRequest, Action onDispose, CancellationToken cancellationToken = default)
        {
            m_DisposableAsyncOperation = new DisposableAsyncOperation<T>(asyncOperation, cancellationToken);
            m_OnAsyncDisposeRequest = onAsyncDisposeRequest;
            m_OnDispose = onDispose;
        }

        protected override void OnDispose()
        {
            m_OnDispose?.Invoke();
        }

        protected override UniTask OnDisposeAsync(CancellationToken cancellationToken = default)
        {
            m_DisposableAsyncOperation.Dispose();
            return m_OnAsyncDisposeRequest.Invoke();
        }
        
        
    }
}