using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Metamatic
{
    public abstract class RunnableBehaviour : MonoBehaviour, IRunnable
    {
        private CancellationTokenSource m_CancellationTokenSource;
        private bool m_IsDisposed = false;
        private TaskCompletionSource<bool> m_CancellationProcessTaslCompletionSource = null;
        public void Dispose()
        {
            if (m_IsDisposed) return;

            m_IsDisposed = true;
            m_CancellationTokenSource?.Cancel();
            m_CancellationTokenSource = null;
            OnDispose();
        }
        
        /// <summary>
        /// Run behaviour automatically after game start
        /// </summary>
        private void Start()
        {
            if (!ShouldRunOnStart()) return;
            var cancellationTokenSource = new CancellationTokenSource();
            Run(cancellationTokenSource.Token);
        }

        public async Task Run(CancellationToken cancellationToken = default)
        {
            if (m_CancellationTokenSource != null)
            {
                if (m_CancellationTokenSource.IsCancellationRequested)
                {
                    /*
                     * If cancellation token was cancelled, we need to wait a bit previous script is complete
                     */
                    if (m_CancellationProcessTaslCompletionSource != null)
                    {
                        Debug.LogWarning("To many run call requests on the same RunnableBehaviour script");
                        throw new InvalidOperationException("To many run call requests on the same RunnableBehaviour script");
                    }
                    else
                    {
                        m_CancellationProcessTaslCompletionSource = new TaskCompletionSource<bool>();
                        await m_CancellationProcessTaslCompletionSource.Task;
                        m_CancellationProcessTaslCompletionSource = null;
                    }
                }
                else
                {
                    Debug.LogWarning("You can not run behaviour before previous run did complete");
                    throw new InvalidOperationException("You can not run behaviour before previous run did complete");
                }
            }
            m_CancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            try
            {
                await RunBehaviour(m_CancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning("RunnableBehaviour is cancelled");
            }
            catch (Exception e)
            {
                throw e;
            }
            
            m_CancellationTokenSource = null;
            m_CancellationProcessTaslCompletionSource?.SetResult(true);
        }

        private void OnDestroy()
        {
            Dispose();
        }

        protected abstract Task RunBehaviour(CancellationToken cancellationToken);
        protected virtual void OnDispose() {}

        protected virtual bool ShouldRunOnStart()
        {
            return false;
        }
    }
}