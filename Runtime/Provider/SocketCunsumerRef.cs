using System;
using System.Threading;
using Sirenix.OdinInspector;

namespace Metamatic
{
    [Obsolete("Use plug with autoConnect = true instead.")]
    public class SocketConsumerRef<TSocketScheme>: AsyncBehaviourBase
        where TSocketScheme: SocketScheme, new()
    {
        public Plug<TSocketScheme> plug;
        [HideInEditorMode, ShowInInspector, ReadOnly]
        public SocketProviderRef<TSocketScheme> m_Provider { get; private set; }
        private SequentialHandler<SocketProviderRef<TSocketScheme>> m_ProviderHandler;
        private CancellationTokenSource m_DisableCancellationTokenSource;
        protected sealed override void OnEnable()
        {
            base.OnEnable();
            m_DisableCancellationTokenSource = new CancellationTokenSource();
            m_ProviderHandler = new SequentialHandler<SocketProviderRef<TSocketScheme>>(HandleProviderChange, m_DisableCancellationTokenSource.Token);
            m_DisableCancellationTokenSource.Token.Register(SocketProviderRef<TSocketScheme>.instance.Subscribe(m_ProviderHandler.Handle));
        }

        protected sealed override void OnDisable()
        {
            base.OnDisable();
            m_DisableCancellationTokenSource.Cancel();
            m_DisableCancellationTokenSource.Dispose();
        }

        protected override void RunAsync(CancellationToken cancellationToken)
        {
            
        }

        private void HandleProviderChange(SocketProviderRef<TSocketScheme> provider, CancellationToken cancellationToken)
        {
            if (provider != null && provider.socket != null && plug != null)
            {
                m_Provider = provider;
                cancellationToken.Register(provider.socket.Connect(this.plug));
            }
        }
    }
}