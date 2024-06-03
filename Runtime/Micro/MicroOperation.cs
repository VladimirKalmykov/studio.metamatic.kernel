using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Pool;

namespace Metamatic
{
    public class MicroOperation: IDisposable, IAsyncDisposable
    {
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
        
        private CancellationTokenSource m_DisposeCancellationTokenSource;
        private UniTask? m_Task;
        
        private MicroOperation()
        {
            
        }
        
        public delegate UniTask AsyncAction(CancellationToken cancellationToken);
        protected MicroOperation(AsyncAction action): this()
        {
            InternalRun(action);
        }
        ~MicroOperation()
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
            if (m_Task.HasValue)
            {
                try
                {
                    await m_Task.Value;
                }
                finally
                {
                    m_Task = null;
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
        
    }
}