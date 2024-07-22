using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic
{
    public class MicroOperation: IDisposable, IAsyncDisposable
    {
        public delegate UniTask AsyncAction(CancellationToken cancellationToken);
        public delegate UniTask AsyncOperationAction(MicroOperation operation);
        private static Queue<MicroOperation> _Pool = new ();
        public static MicroOperation Run(AsyncAction action)
        {
            if (_Pool.Count > 0)
            {
                var operation = _Pool.Dequeue();
                operation.InternalRun(action);
                return operation;
            }
            return new MicroOperation(action);
        }
        
        public static MicroOperation Run(AsyncOperationAction action)
        {
            if (_Pool.Count > 0)
            {
                var operation = _Pool.Dequeue();
                operation.m_Disposed = false;
                operation.InternalRun(action);
                return operation;
            }
            return new MicroOperation(action);
        }
        
        private CancellationTokenSource m_DisposeCancellationTokenSource;
        public CancellationToken cancellationToken => m_DisposeCancellationTokenSource.Token;
        private UniTask? m_Task;
        private bool m_IsSyncDisposed;
        public bool isSyncDisposed => m_IsSyncDisposed;
        public bool isAsyncDisposed => !m_IsSyncDisposed;
        
        private MicroOperation()
        {
            
        }
        
        
        protected MicroOperation(AsyncAction action): this()
        {
            InternalRun(action);
        }
        protected MicroOperation(AsyncOperationAction action): this()
        {
            InternalRun(action);
        }
        ~MicroOperation()
        {
            Dispose();
        }
        private bool m_Disposed;
        public async ValueTask DisposeAsync()
        {
            if (m_Disposed)
            {
                throw new InvalidOperationException("Operation already disposed");
            }
            Dispose(false);
            if (m_Task.HasValue)
            {
                try
                {
                    await m_Task.Value;
                }
                finally
                {
                    m_Task = null;
                    m_IsSyncDisposed = false;
                    m_DisposeCancellationTokenSource.Dispose();
                    m_DisposeCancellationTokenSource = null;
                    if (_Pool.Count < 100)
                    {
                        m_Disposed = false;
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
                throw new InvalidOperationException("Already disposed");
                return;
            }

            Debug.Log("[OPERATION] Dispose");
            m_Disposed = true;
            m_IsSyncDisposed = isSync;
            m_DisposeCancellationTokenSource?.Cancel();
            if (isSync)
            {
                m_Task = null;
                m_IsSyncDisposed = false;
                m_DisposeCancellationTokenSource = null;
                if (_Pool.Count < 100) {
                    /* Reset before enqueuing */
                    m_Disposed = false;
                    
                    _Pool.Enqueue(this);
                }
            }
            GC.SuppressFinalize(this);
        }

        private async void InternalRun(AsyncAction action)
        {
            if (m_Task.HasValue)
            {
                throw new InvalidOperationException("Task is already running");
            }
            var taskCompletionSource = new UniTaskCompletionSource();
            m_Task = taskCompletionSource.Task;
            m_DisposeCancellationTokenSource = new CancellationTokenSource();
            var task = action(m_DisposeCancellationTokenSource.Token);
            try
            {
                await task;
            } catch (OperationCanceledException)
            {
                // Skip
            }
            finally
            {
                taskCompletionSource.TrySetResult();
            }
        }
        
        private async void InternalRun(AsyncOperationAction action)
        {
            if (m_Task.HasValue)
            {
                throw new InvalidOperationException("Task is already running");
            }
            var taskCompletionSource = new UniTaskCompletionSource();
            m_Task = taskCompletionSource.Task;
            m_DisposeCancellationTokenSource = new CancellationTokenSource();
            var task = action(this);
            try
            {
                await task;
            } catch (OperationCanceledException)
            {
                // Skip
            }
            finally
            {
                taskCompletionSource.TrySetResult();
            }
        }
    }
    
    /// <summary>
    /// Similar to MicroOperation, but with one parameter
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class MicroOperation<T> : IDisposable, IAsyncDisposable
    {
        public delegate UniTask AsyncAction(T value, CancellationToken cancellationToken);
        public delegate UniTask AsyncOperationAction(T value, MicroOperation<T> operation);
        private static Queue<MicroOperation<T>> _Pool = new ();
        public static MicroOperation<T> Run(T value, AsyncAction action)
        {
            if (_Pool.Count > 0)
            {
                var operation = _Pool.Dequeue();
                operation.InternalRun(action, value);
                return operation;
            }
            return new MicroOperation<T>(value, action);
        }
        
        public static MicroOperation<T> Run(T value, AsyncOperationAction action)
        {
            if (_Pool.Count > 0)
            {
                var operation = _Pool.Dequeue();
                operation.m_Disposed = false;
                operation.InternalRun(action, value);
                return operation;
            }
            return new MicroOperation<T>(value, action);
        }
        
        private CancellationTokenSource m_DisposeCancellationTokenSource;
        public CancellationToken cancellationToken => m_DisposeCancellationTokenSource.Token;
        private UniTask? m_Task;
        private bool m_IsSyncDisposed;
        public bool isSyncDisposed => m_IsSyncDisposed;
        public bool isAsyncDisposed => !m_IsSyncDisposed;
        
        private MicroOperation()
        {
            
        }
        
        
        protected MicroOperation(T value, AsyncAction action): this()
        {
            InternalRun(action, value);
        }
        protected MicroOperation(T value, AsyncOperationAction action): this()
        {
            InternalRun(action, value);
        }
        ~MicroOperation()
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
            if (m_Task.HasValue)
            {
                try
                {
                    await m_Task.Value;
                }
                finally
                {
                    m_Task = null;
                    m_IsSyncDisposed = false;
                    m_DisposeCancellationTokenSource.Dispose();
                    m_DisposeCancellationTokenSource = null;
                    if (_Pool.Count < 100) {
                        m_Disposed = false;
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
                m_Task = null;
                m_IsSyncDisposed = false;
                m_DisposeCancellationTokenSource = null;
                if (_Pool.Count < 100) {
                    m_Disposed = false;
                    _Pool.Enqueue(this);
                }
            }
            GC.SuppressFinalize(this);
        }

        private async void InternalRun(AsyncAction action, T value)
        {
            if (m_Task.HasValue)
            {
                throw new InvalidOperationException("Task is already running");
            }
            var taskCompletionSource = new UniTaskCompletionSource();
            m_Task = taskCompletionSource.Task;
            m_DisposeCancellationTokenSource = new CancellationTokenSource();
            var task = action(value, m_DisposeCancellationTokenSource.Token);
            try
            {
                await task;
            } catch (OperationCanceledException)
            {
                // Skip
            }
            finally
            {
                taskCompletionSource.TrySetResult();
            }
        }
        
        private async void InternalRun(AsyncOperationAction action, T value)
        {
            if (m_Task.HasValue)
            {
                throw new InvalidOperationException("Task is already running");
            }
            var taskCompletionSource = new UniTaskCompletionSource();
            m_Task = taskCompletionSource.Task;
            m_DisposeCancellationTokenSource = new CancellationTokenSource();
            var task = action(value, this);
            try
            {
                await task;
            } catch (OperationCanceledException)
            {
                // Skip
            }
            finally
            {
                taskCompletionSource.TrySetResult();
            }
        }
    }
}