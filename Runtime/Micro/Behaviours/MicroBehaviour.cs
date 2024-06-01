using System;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic
{
    public abstract class MicroBehaviour: IAsyncDisposable, IDisposable
    {
        protected MicroBehaviour()
        {
            
        }
        ~MicroBehaviour()
        {
            Dispose();
        }
        public async ValueTask DisposeAsync()
        {
            Dispose();
            if (m_Task.HasValue)
            {
                await m_Task.Value;
            }
        }

        public virtual void Dispose()
        {
            m_DisposeCancellationTokenSource?.Cancel();
            GC.SuppressFinalize(this);
        }
        
        private CancellationTokenSource m_DisposeCancellationTokenSource;
        private UniTask? m_Task;
        
        protected delegate UniTask AsyncAction(CancellationToken cancellationToken);

        protected async void Run()
        {
            if (m_Task.HasValue)
            {
                throw new InvalidOperationException("Task is already running");
            }
            var taskCompletionSource = new UniTaskCompletionSource();
            m_Task = taskCompletionSource.Task;
            m_DisposeCancellationTokenSource = new CancellationTokenSource();
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
            }
            finally
            {
                taskCompletionSource.TrySetResult();
            }
        }

        protected abstract UniTask Main(CancellationToken cancellationToken);
    }
}