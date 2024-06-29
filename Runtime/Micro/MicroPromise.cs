using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic
{
    public class MicroPromise: IDisposable, IAsyncDisposableResultable
    {
        #region Static
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
        private string m_DebugName;
        public static MicroPromise Run(AsyncActionPromise action, CancellationToken cancellationToken = default)
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
        #endregion
        
        public delegate UniTask AsyncAction(Action resolve, Action<Exception> reject, CancellationToken cancellationToken);
        public delegate UniTask AsyncActionPromise(MicroPromise promise);
        private CancellationTokenSource m_DisposeCancellationTokenSource;
        private bool m_IsSyncDisposed;
        private UniTaskCompletionSource m_ResultTaskCompletionSource;
        private UniTaskCompletionSource m_ReleaseTaskCompletionSource;
        public CancellationToken cancellationToken => m_DisposeCancellationTokenSource.Token;
        public bool isSyncDisposed => m_IsSyncDisposed;
        public bool isAsyncDisposed => !m_IsSyncDisposed;
        public UniTask result => m_ResultTaskCompletionSource?.Task ?? UniTask.CompletedTask;
        public UniTask release => m_ReleaseTaskCompletionSource?.Task ?? UniTask.CompletedTask;
        
        
        private MicroPromise()
        {
            /* Generate unique debug name */
            m_DebugName = Guid.NewGuid().ToString();
        }
        
        protected MicroPromise(AsyncAction action, CancellationToken cancellationToken = default): this()
        {
            InternalRun(action, cancellationToken);
        }
        protected MicroPromise(AsyncActionPromise action, CancellationToken cancellationToken = default): this()
        {
            InternalRun(action, cancellationToken);
        }
        ~MicroPromise()
        {
            Dispose();
        }
        private bool m_Disposed;
        public async ValueTask DisposeAsync()
        {
            if (m_Disposed)
            {
                return;
            }
            Dispose(false);
            if (m_ReleaseTaskCompletionSource != null)
            {
                try
                {
                    await m_ReleaseTaskCompletionSource.Task;
                }
                finally
                {
                    m_ResultTaskCompletionSource.TrySetException(new EvaluateException("Result was not set"));
                    m_ResultTaskCompletionSource = null;
                    m_ReleaseTaskCompletionSource = null;
                    m_DisposeCancellationTokenSource.Dispose();
                    m_DisposeCancellationTokenSource = null;
                    // TODO: Make this configurable or automatic adaptive
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

        private void Dispose(bool isFromFinalizer, bool isSync = false)
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
                m_ResultTaskCompletionSource.TrySetException(new EvaluateException("Result was not set"));
                m_ResultTaskCompletionSource = null;
                m_ReleaseTaskCompletionSource = null;
                m_DisposeCancellationTokenSource?.Dispose();
                m_IsSyncDisposed = false;
                m_DisposeCancellationTokenSource = null;
                // TODO: Make this configurable or automatic adaptive
                if (_Pool.Count < 100) {
                    _Pool.Enqueue(this);
                }
            }
            GC.SuppressFinalize(this);
        }

        private async void InternalRun(AsyncAction action, CancellationToken externalCancellationToken)
        {
            if (m_ReleaseTaskCompletionSource != null)
            {
                throw new InvalidOperationException("Task is already running");
            }
            var releaseTaskCompletionSource = m_ReleaseTaskCompletionSource = new UniTaskCompletionSource();
            m_ResultTaskCompletionSource = new UniTaskCompletionSource();
            m_DisposeCancellationTokenSource =
                CancellationTokenSource.CreateLinkedTokenSource(externalCancellationToken);
            var task = action(Resolve, Reject, m_DisposeCancellationTokenSource.Token);
            try
            {
                await task;
            }
            catch (Exception e)
            {
                m_ResultTaskCompletionSource.TrySetException(e);
            }
            finally
            {
                releaseTaskCompletionSource?.TrySetResult();
            }
        }
        
        private async void InternalRun(AsyncActionPromise action, CancellationToken externalCancellationToken)
        {
            if (m_ReleaseTaskCompletionSource != null)
            {
                throw new InvalidOperationException("Task is already running");
            }
            var releaseTaskCompletionSource = m_ReleaseTaskCompletionSource = new UniTaskCompletionSource();
            m_ResultTaskCompletionSource = new UniTaskCompletionSource();
            m_DisposeCancellationTokenSource =
                CancellationTokenSource.CreateLinkedTokenSource(externalCancellationToken);
            var task = action(this);
            try
            {
                await task;
            }
            catch (Exception e)
            {
                m_ResultTaskCompletionSource.TrySetException(e);
            }
            finally
            {
                releaseTaskCompletionSource?.TrySetResult();
            }
        }
        
        public void Resolve() => m_ResultTaskCompletionSource.TrySetResult();
        public void Reject(Exception e) => m_ResultTaskCompletionSource.TrySetException(e);
        
    }
    
    public class MicroPromise<T> : IDisposable, IAsyncDisposable
    {
        #region Static
        private static Queue<MicroPromise<T>> _Pool = new();
        public static MicroPromise<T> Run(AsyncAction action, CancellationToken cancellationToken = default)
        {
            if (_Pool.Count > 0)
            {
                var operation = _Pool.Dequeue();
                operation.m_Disposed = false;
                operation.InternalRun(action, cancellationToken);
                return operation;
            }
            return new MicroPromise<T>(action);
        }
        public static MicroPromise<T> Run(AsyncActionPromise action, CancellationToken cancellationToken = default)
        {
            if (_Pool.Count > 0)
            {
                var operation = _Pool.Dequeue();
                operation.m_Disposed = false;
                operation.InternalRun(action, cancellationToken);
                return operation;
            }
            return new MicroPromise<T>(action);
        }
        #endregion

        public delegate UniTask AsyncAction(Action<T> resolve, Action<Exception> reject, CancellationToken cancellationToken);
        public delegate UniTask AsyncActionPromise(MicroPromise<T> promise);
        private CancellationTokenSource m_DisposeCancellationTokenSource;
        private bool m_IsSyncDisposed;
        private UniTaskCompletionSource<T> m_ResultTaskCompletionSource;
        private UniTaskCompletionSource m_ReleaseTaskCompletionSource;
        public CancellationToken cancellationToken => m_DisposeCancellationTokenSource.Token;
        public bool isSyncDisposed => m_IsSyncDisposed;
        public bool isAsyncDisposed => !m_IsSyncDisposed;
        public UniTask<T> result => m_ResultTaskCompletionSource?.Task ?? UniTask.FromResult(default(T));
        public UniTask release => m_ReleaseTaskCompletionSource?.Task ?? UniTask.CompletedTask;

        private MicroPromise() { }

        protected MicroPromise(AsyncAction action, CancellationToken cancellationToken = default) : this()
        {
            InternalRun(action, cancellationToken);
        }
        protected MicroPromise(AsyncActionPromise action, CancellationToken cancellationToken = default) : this()
        {
            InternalRun(action, cancellationToken);
        }
        ~MicroPromise()
        {
            Dispose();
        }
        private bool m_Disposed;
        public async ValueTask DisposeAsync()
        {
            if (m_Disposed)
            {
                return;
            }
            Dispose(false);
            if (m_ReleaseTaskCompletionSource != null)
            {
                try
                {
                    await m_ReleaseTaskCompletionSource.Task;
                }
                finally
                {
                    m_ResultTaskCompletionSource.TrySetException(new EvaluateException("Result was not set"));
                    m_ResultTaskCompletionSource = null;
                    m_ReleaseTaskCompletionSource = null;
                    m_DisposeCancellationTokenSource.Dispose();
                    m_DisposeCancellationTokenSource = null;
                    // TODO: Make this configurable or automatic adaptive
                    if (_Pool.Count < 100)
                    {
                        _Pool.Enqueue(this);
                    }
                }
            }
        }

        public void Dispose()
        {
            Dispose(false, true);
        }

        private void Dispose(bool isFromFinalizer, bool isSync = false)
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
                m_ResultTaskCompletionSource.TrySetException(new EvaluateException("Result was not set"));
                m_ResultTaskCompletionSource = null;
                m_ReleaseTaskCompletionSource = null;
                m_DisposeCancellationTokenSource?.Dispose();
                m_IsSyncDisposed = false;
                m_DisposeCancellationTokenSource = null;
                // TODO: Make this configurable or automatic adaptive
                if (_Pool.Count < 100)
                {
                    _Pool.Enqueue(this);
                }
            }
            GC.SuppressFinalize(this);
        }

        private async void InternalRun(AsyncAction action, CancellationToken externalCancellationToken)
        {
            if (m_ReleaseTaskCompletionSource != null)
            {
                throw new InvalidOperationException("Task is already running");
            }
            var releaseTaskCompletionSource = m_ReleaseTaskCompletionSource = new UniTaskCompletionSource();
            m_ResultTaskCompletionSource = new UniTaskCompletionSource<T>();
            m_DisposeCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(externalCancellationToken);
            var task = action(Resolve, Reject, m_DisposeCancellationTokenSource.Token);
            try
            {
                await task;
            }
            catch (Exception e)
            {
                m_ResultTaskCompletionSource.TrySetException(e);
            }
            finally
            {
                releaseTaskCompletionSource?.TrySetResult();
            }
        }

        private async void InternalRun(AsyncActionPromise action, CancellationToken externalCancellationToken)
        {
            if (m_ReleaseTaskCompletionSource != null)
            {
                throw new InvalidOperationException("Task is already running");
            }
            var releaseTaskCompletionSource = m_ReleaseTaskCompletionSource = new UniTaskCompletionSource();
            m_ResultTaskCompletionSource = new UniTaskCompletionSource<T>();
            m_DisposeCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(externalCancellationToken);
            var task = action(this);
            try
            {
                await task;
            }
            catch (Exception e)
            {
                m_ResultTaskCompletionSource.TrySetException(e);
            }
            finally
            {
                releaseTaskCompletionSource?.TrySetResult();
            }
        }

        public void Resolve(T result) => m_ResultTaskCompletionSource.TrySetResult(result);
        public void Reject(Exception e) => m_ResultTaskCompletionSource.TrySetException(e);
    }

}