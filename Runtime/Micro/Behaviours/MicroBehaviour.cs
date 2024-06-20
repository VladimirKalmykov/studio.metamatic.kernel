using System;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic
{
    public abstract class MicroBehaviour: IAsyncDisposable, IDisposable
    {
        private bool m_IsSyncDisposed;
        public bool isSyncDisposed => m_IsSyncDisposed;
        public bool isAsyncDisposed => !m_IsSyncDisposed;
        private bool m_Disposed;
        private CancellationTokenSource m_DisposeCancellationTokenSource;
        public CancellationToken cancellationToken => m_DisposeCancellationTokenSource.Token;
        private UniTask? m_Task;
        protected MicroBehaviour()
        {
            
        }
        ~MicroBehaviour()
        {
            Dispose(true);
        }
        public async ValueTask DisposeAsync()
        {
            if (m_Disposed)
            {
                return;
            }
            Dispose(false);
            if (m_Task.HasValue)
            {
                await m_Task.Value;
            }
        }

        public void Dispose()
        {
            Dispose(false, true);
        }
        
        private void Dispose(bool isFromFinalizer = false, bool isSync = false)
        {
            if (isFromFinalizer)
            {
                Debug.LogWarning("You should dispose MicroOperation manually");
            }
            if (m_Disposed)
            {
                return;
            }
            m_IsSyncDisposed = isSync;
            m_Disposed = true;
            m_DisposeCancellationTokenSource?.Cancel();
            GC.SuppressFinalize(this);
        }
        
        protected delegate UniTask AsyncAction(CancellationToken cancellationToken);

        protected async void Run(CancellationToken cancellationToken = default)
        {
            if (m_Task.HasValue)
            {
                throw new InvalidOperationException("Task is already running");
            }
            var taskCompletionSource = new UniTaskCompletionSource();
            m_Task = taskCompletionSource.Task;
            m_DisposeCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var task = Main(m_DisposeCancellationTokenSource.Token);
            try
            {
                await task;
            } catch (OperationCanceledException)
            {
                // Skip
            }
            catch (Exception e)
            {
                taskCompletionSource.TrySetException(e);
            }
            finally
            {
                taskCompletionSource.TrySetResult();
            }
        }

        protected abstract UniTask Main(CancellationToken cancellationToken);
    }
}