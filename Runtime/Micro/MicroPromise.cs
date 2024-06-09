using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Pool;

namespace Metamatic
{
    public class MicroPromise: IDisposable, IAsyncDisposableResultable
    {
        private static Queue<MicroPromise> _Pool = new ();
        public static MicroPromise Run(AsyncAction action, CancellationToken cancellationToken = default)
        {
            if (_Pool.Count > 0)
            {
                var operation = _Pool.Dequeue();
                operation.m_Disposed = false;
                operation.InternalRun(action, cancellationToken);
                return operation;
            }
            return new MicroPromise(action);
        }
        
        private CancellationTokenSource m_DisposeCancellationTokenSource;
        private UniTask? m_ResultTask;
        private UniTask? m_ReleaseTask;
        private bool m_IsSyncDisposed = false;
        public bool isSyncDisposed => m_IsSyncDisposed;
        public bool isAsyncDisposed => !m_IsSyncDisposed;
        
        public UniTask result => m_ResultTask ?? UniTask.CompletedTask;
        
        private MicroPromise()
        {
            
        }
        
        public delegate UniTask AsyncAction(Action resolve, Action<Exception> reject, CancellationToken cancellationToken);
        protected MicroPromise(AsyncAction action, CancellationToken cancellationToken = default): this()
        {
            InternalRun(action, cancellationToken);
        }
        ~MicroPromise()
        {
            Dispose();
        }
        private bool m_Disposed = false;
        public async ValueTask DisposeAsync()
        {
            if (m_Disposed)
            {
                return;
            }
            Dispose(false);
            if (m_ReleaseTask.HasValue)
            {
                try
                {
                    await m_ReleaseTask.Value;
                }
                finally
                {
                    m_ReleaseTask = null;
                    m_ResultTask = null;
                    m_DisposeCancellationTokenSource.Dispose();
                    m_DisposeCancellationTokenSource = null;
                    if (_Pool.Count < 100) {
                        _Pool.Enqueue(this);
                    }
                }
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
            m_Disposed = true;
            m_IsSyncDisposed = isSync;
            m_DisposeCancellationTokenSource?.Cancel();
            if (isSync)
            {
                m_ReleaseTask = null;
                m_ResultTask = null;
                m_IsSyncDisposed = false;
                m_DisposeCancellationTokenSource = null;
                if (_Pool.Count < 100) {
                    _Pool.Enqueue(this);
                }
            }
            GC.SuppressFinalize(this);
        }

        private async void InternalRun(AsyncAction action, CancellationToken externalCancellationToken)
        {
            if (m_ReleaseTask.HasValue)
            {
                throw new InvalidOperationException("Task is already running");
            }
            var releaseTaskCompletionSource = new UniTaskCompletionSource();
            var resultTaskCompletionSource = new UniTaskCompletionSource();
            m_ReleaseTask = releaseTaskCompletionSource.Task;
            m_ResultTask = resultTaskCompletionSource.Task;
            m_DisposeCancellationTokenSource =
                CancellationTokenSource.CreateLinkedTokenSource(externalCancellationToken);
            var task = action(() => resultTaskCompletionSource.TrySetResult(),
                (Exception e) => resultTaskCompletionSource.TrySetException(e), m_DisposeCancellationTokenSource.Token);
            try
            {
                await task;
            } catch (OperationCanceledException)
            {
                // Skip
                resultTaskCompletionSource.TrySetException(new OperationCanceledException("Operation canceled by MicroPromise"));
            }
            finally
            {
                resultTaskCompletionSource.TrySetException(new EvaluateException("Result task is not set"));
                releaseTaskCompletionSource.TrySetResult();
            }
        }
        
    }
    
    public class MicroPromise<T>: IDisposable, IAsyncDisposableResultable<T>
    {
        private static Queue<MicroPromise<T>> _Pool = new ();
        public static MicroPromise<T> Run(AsyncAction action, CancellationToken cancellationToken = default)
        {
            if (_Pool.Count > 0)
            {
                var operation = _Pool.Dequeue();
                operation.InternalRun(action, cancellationToken);
                return operation;
            }
            return new MicroPromise<T>(action);
        }
        
        private CancellationTokenSource m_DisposeCancellationTokenSource;
        private UniTask<T>? m_ResultTask;
        private UniTask? m_ReleaseTask;
        
        public UniTask<T> result => m_ResultTask ?? default;
        
        private MicroPromise()
        {
            
        }
        
        public delegate UniTask AsyncAction(Action<T> resolve, Action<Exception> reject, CancellationToken cancellationToken);
        protected MicroPromise(AsyncAction action, CancellationToken cancellationToken = default): this()
        {
            InternalRun(action, cancellationToken);
        }
        ~MicroPromise()
        {
            Dispose();
        }
        private bool m_Disposed = false;
        public async ValueTask DisposeAsync()
        {
            if (m_Disposed)
            {
                return;
            }
            Dispose();
            if (m_ReleaseTask.HasValue)
            {
                try
                {
                    await m_ReleaseTask.Value;
                }
                finally
                {
                    m_ReleaseTask = null;
                    m_ResultTask = null;
                    m_Disposed = false;
                    m_DisposeCancellationTokenSource.Dispose();
                    m_DisposeCancellationTokenSource = null;
                    if (_Pool.Count < 100) {
                        _Pool.Enqueue(this);
                    }
                }
            }
        }
        
        public void Dispose()
        {
            Dispose(false);
        }

        private void Dispose(bool isFromFinalizer = false)
        {
            if (isFromFinalizer)
            {
                Debug.LogWarning("You should dispose MicroOperation manually");
            }
            if (m_Disposed)
            {
                return;
            }
            m_Disposed = true;
            m_DisposeCancellationTokenSource?.Cancel();
            GC.SuppressFinalize(this);
        }

        private async void InternalRun(AsyncAction action, CancellationToken externalCancellationToken)
        {
            if (m_ReleaseTask.HasValue)
            {
                throw new InvalidOperationException("Task is already running");
            }
            var releaseTaskCompletionSource = new UniTaskCompletionSource();
            var resultTaskCompletionSource = new UniTaskCompletionSource<T>();
            m_ReleaseTask = releaseTaskCompletionSource.Task;
            m_ResultTask = resultTaskCompletionSource.Task;
            m_DisposeCancellationTokenSource =
                CancellationTokenSource.CreateLinkedTokenSource(externalCancellationToken);
            var task = action((result) => resultTaskCompletionSource.TrySetResult(result),
                (Exception e) => resultTaskCompletionSource.TrySetException(e), m_DisposeCancellationTokenSource.Token);
            try
            {
                await task;
            } catch (OperationCanceledException)
            {
                // Skip
                resultTaskCompletionSource.TrySetException(new OperationCanceledException("Operation canceled by MicroPromise"));
            }
            finally
            {
                resultTaskCompletionSource.TrySetException(new EvaluateException("Result task is not set"));
                releaseTaskCompletionSource.TrySetResult();
            }
        }
    }
}