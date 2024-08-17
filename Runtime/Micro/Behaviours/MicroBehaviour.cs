using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic
{
    public abstract class MicroBehaviour: IUniTaskAsyncDisposable, IDisposable
    {
        private bool m_IsSyncDisposed;
        public bool isSyncDisposed => m_IsSyncDisposed;
        public bool isAsyncDisposed => !m_IsSyncDisposed;
        private bool m_Disposed;
        private CancellationTokenSource m_DisposeCancellationTokenSource;
        public CancellationToken disposeCancellationToken => m_DisposeCancellationTokenSource.Token;
        private readonly UniTaskCompletionSource m_TaskCompletionSource = new();
        private bool m_IsRunning;
        ~MicroBehaviour()
        {
            Dispose(true);
        }
        public async UniTask DisposeAsync()
        {
            if (m_Disposed)
            {
                return;
            }
            Dispose(false);
            await m_TaskCompletionSource.Task;
        }

        public void Dispose()
        {
            Dispose(false, true);
        }
        
        private void Dispose(bool isFromFinalizer, bool isSync = false)
        {
            if (isFromFinalizer)
            {
                throw new Exception("You should dispose MicroOperation manually to avoid jumping to finalizer thread");
            }
            if (m_Disposed)
            {
                return;
            }
            m_IsSyncDisposed = isSync;
            m_Disposed = true;
            m_DisposeCancellationTokenSource?.CancelAndDispose();
            GC.SuppressFinalize(this);
        }
        
        protected delegate UniTask AsyncAction(CancellationToken cancellationToken);

        protected async void Run(CancellationToken externalCancellationToken = default)
        {
            if (m_IsRunning)
            {
                throw new InvalidOperationException("Task is already running");
            }
            m_IsRunning = true;
            m_DisposeCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(externalCancellationToken);
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
                Debug.LogError(e);
                m_TaskCompletionSource.TrySetException(e);
            }
            finally
            {
                m_TaskCompletionSource.TrySetResult();
            }
        }

        protected abstract UniTask Main(CancellationToken cancellationToken);
    }
}