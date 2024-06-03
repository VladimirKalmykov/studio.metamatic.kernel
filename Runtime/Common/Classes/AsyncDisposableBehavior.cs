using System;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    [Obsolete("use MicroBehaviour instead")]
    public class AsyncDisposableBehavior: IAsyncDisposable, IDisposable
    {
        public delegate UniTask ProcessHandler(Action resolve, Action<Exception> reject, CancellationToken token);
        private CancellationTokenSource m_DisposeCancellationTokenSource;
        private UniTaskCompletionSource m_TaskCompletionSource;
        private UniTask m_DisposeTask;
        public UniTask Result => m_TaskCompletionSource.Task;
        private bool isAsyncDisposeRequested = false;

        public AsyncDisposableBehavior(ProcessHandler handler, CancellationToken cancellationToken = default)
        {
            m_DisposeCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            m_TaskCompletionSource = new UniTaskCompletionSource();
            m_DisposeTask = handler(Resolve, Reject, m_DisposeCancellationTokenSource.Token);
        }
        private void Resolve()
        {
            m_TaskCompletionSource.TrySetResult();
        }
        private void Reject(Exception e)
        {
            m_TaskCompletionSource.TrySetException(e);
        }
        public async ValueTask DisposeAsync()
        {
            isAsyncDisposeRequested = true;
            m_DisposeCancellationTokenSource.Cancel();
            try
            {
                await m_DisposeTask;
            } catch (OperationCanceledException)
            {
                // Skip
            }

            m_DisposeCancellationTokenSource.Dispose();
            m_TaskCompletionSource.TrySetCanceled();
        }

        public void Dispose()
        {
            isAsyncDisposeRequested = false;
            m_DisposeCancellationTokenSource.Cancel();
            m_DisposeCancellationTokenSource.Dispose();
            m_TaskCompletionSource.TrySetCanceled();
        }
    }
    public class AsyncDisposableBehavior<T>: IAsyncDisposable, IDisposable
    {
        public delegate UniTask ProcessHandler(Action<T> resolve, Action<Exception> reject, CancellationToken token);
        private CancellationTokenSource m_DisposeCancellationTokenSource;
        private UniTaskCompletionSource<T> m_TaskCompletionSource;
        private UniTask m_DisposeTask;
        public UniTask<T> Result => m_TaskCompletionSource.Task;
        private bool isAsyncDisposeRequested = false;

        public AsyncDisposableBehavior(ProcessHandler handler, CancellationToken cancellationToken = default)
        {
            m_DisposeCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            m_TaskCompletionSource = new UniTaskCompletionSource<T>();
            m_DisposeTask = handler(Resolve, Reject, m_DisposeCancellationTokenSource.Token);
        }
        private void Resolve(T value)
        {
            m_TaskCompletionSource.TrySetResult(value);
        }
        private void Reject(Exception e)
        {
            m_TaskCompletionSource.TrySetException(e);
        }
        public async ValueTask DisposeAsync()
        {
            isAsyncDisposeRequested = true;
            m_DisposeCancellationTokenSource.Cancel();
            try
            {
                await m_DisposeTask;
            } catch (OperationCanceledException)
            {
                // Skip
            }

            m_DisposeCancellationTokenSource.Dispose();
            m_TaskCompletionSource.TrySetCanceled();
        }

        public void Dispose()
        {
            isAsyncDisposeRequested = false;
            m_DisposeCancellationTokenSource.Cancel();
            m_DisposeCancellationTokenSource.Dispose();
            m_TaskCompletionSource.TrySetCanceled();
        }
        
        public UniTask<T>.Awaiter GetAwaiter() => Result.GetAwaiter();
    }
}