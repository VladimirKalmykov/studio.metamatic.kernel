using System;
using System.Threading;
using UnityEngine;

namespace Metamatic
{
    [Obsolete("Use Socket with global settings instead.", true)]
    public class SocketConsumer<TSocketScheme>: Plug<TSocketScheme>
        where TSocketScheme: SocketScheme, new()
    {
        private ExclusiveExecution<SocketProvider<TSocketScheme>> m_ProviderExecution;
        private CancellationTokenSource m_DisableCancellationTokenSource;
        protected sealed override void OnEnable()
        {
            base.OnEnable();
            m_DisableCancellationTokenSource = new CancellationTokenSource();
            m_ProviderExecution = new ExclusiveExecution<SocketProvider<TSocketScheme>>(HandleProviderChange, m_DisableCancellationTokenSource.Token);
            m_DisableCancellationTokenSource.Token.Register(SocketProvider<TSocketScheme>.instance.Subscribe(m_ProviderExecution.Handle));
        }

        protected sealed override void OnDisable()
        {
            base.OnDisable();
            m_DisableCancellationTokenSource.Cancel();
        }
        
        private void HandleProviderChange(SocketProvider<TSocketScheme> provider, CancellationToken cancellationToken)
        {
            m_ProviderExecution?.Dispose();
            if (provider != null)
            {
                cancellationToken.Register(provider.Connect(this));
            }
        }
    }
}