using System.Threading;
using UnityEngine;

namespace Metamatic
{
    public class Consumer<TSocketScheme>: Plug<TSocketScheme>
        where TSocketScheme: SocketScheme, new()
    {
        private SequentialHandler<Provider<TSocketScheme>, object> m_ProviderHandler;
        private CancellationTokenSource m_DisableCancellationTokenSource;
        protected sealed override void OnEnable()
        {
            base.OnEnable();
            m_DisableCancellationTokenSource = new CancellationTokenSource();
            m_ProviderHandler = new SequentialHandler<Provider<TSocketScheme>, object>(HandleProviderChange, m_DisableCancellationTokenSource.Token);
            m_DisableCancellationTokenSource.Token.Register(Provider<TSocketScheme>.instance.Subscribe(m_ProviderHandler.Handle));
        }

        protected sealed override void OnDisable()
        {
            base.OnDisable();
            m_DisableCancellationTokenSource.Cancel();
        }
        
        private void HandleProviderChange(Provider<TSocketScheme> provider, CancellationToken cancellationToken, object _)
        {
            if (provider != null)
            {
                cancellationToken.Register(provider.socket.Connect(this));
            }
        }
    }
}