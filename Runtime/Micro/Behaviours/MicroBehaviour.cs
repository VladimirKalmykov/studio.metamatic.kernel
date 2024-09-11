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
        private CancellationSignalSource m_DisposeCancellationSignalSource;
        public CancellationSignal disposeSignal => m_DisposeCancellationSignalSource.Signal;
        private readonly SafeTaskCompletionSource m_TaskCompletionSource = SafeTaskCompletionSource.Create();
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
                Debug.LogError("You should dispose MicroOperation manually to avoid jumping to finalizer thread");
            }
            if (m_Disposed)
            {
                return;
            }
            m_IsSyncDisposed = isSync;
            m_Disposed = true;
            m_DisposeCancellationSignalSource.TryCancel();
            GC.SuppressFinalize(this);
        }
        
        protected delegate UniTask AsyncAction(CancellationSignal signal);

        protected async void Run(CancellationSignal externalCancellationSignal = default)
        {
            if (m_IsRunning)
            {
                throw new InvalidOperationException("Task is already running");
            }
            m_IsRunning = true;
            m_DisposeCancellationSignalSource = CancellationSignalSource.CreateLinked(externalCancellationSignal);
            var task = Main(m_DisposeCancellationSignalSource.Signal);
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

        protected abstract UniTask Main(CancellationSignal signal);
    }
}