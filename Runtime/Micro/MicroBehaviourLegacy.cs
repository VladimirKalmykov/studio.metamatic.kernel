using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic
{
    public abstract class MicroBehaviourLegacy: IUniTaskAsyncDisposable, IDisposable
    {
        private bool m_IsSyncDisposed;
        public bool isSyncDisposed => m_IsSyncDisposed;
        public bool isAsyncDisposed => !m_IsSyncDisposed;
        private bool m_Disposed;
        private SignalSource m_DisposeCancellationSignalSource;
        public Signal disposeSignal => m_DisposeCancellationSignalSource.Signal;
        private readonly SafeUniTaskCompletionSource m_UniTaskCompletionSource = SafeUniTaskCompletionSource.Create();
        private bool m_IsRunning;
        ~MicroBehaviourLegacy()
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
            await m_UniTaskCompletionSource.Task;
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
            m_DisposeCancellationSignalSource.TryRaise();
            GC.SuppressFinalize(this);
        }
        
        protected delegate UniTask AsyncAction(Signal signal);

        protected async void Run(Signal externalCancellationSignal = default)
        {
            if (m_IsRunning)
            {
                throw new InvalidOperationException("Task is already running");
            }
            m_IsRunning = true;
            m_DisposeCancellationSignalSource = SignalSource.CreateLinked(externalCancellationSignal);
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
                m_UniTaskCompletionSource.TrySetException(e);
            }
            finally
            {
                m_UniTaskCompletionSource.TrySetResult();
            }
        }

        protected abstract UniTask Main(Signal signal);
    }
}