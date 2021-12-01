using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Metamatic
{
    public abstract class RunnableBehaviour : MonoBehaviour, IRunnable
    {
        private CancellationTokenSource m_CancellationTokenSource;
        public void Dispose()
        {
            m_CancellationTokenSource?.Cancel();
            m_CancellationTokenSource = null;
            OnDispose();
        }

        public async Task Run(CancellationToken cancellationToken = default)
        {
            if (m_CancellationTokenSource != null)
            {
                throw new InvalidOperationException("You can not run behaviour before previous run did complete");
            }
            m_CancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            await RunBehaviour(m_CancellationTokenSource.Token);
            Debug.Log("Behaviour complete");
            m_CancellationTokenSource = null;
        }

        protected abstract Task RunBehaviour(CancellationToken cancellationToken);
        protected virtual void OnDispose() {}
    }
}