using System;
using Cysharp.Threading.Tasks;
using Metamatic;

namespace Metamatic
{
    public readonly struct NullSafeAsyncDisposableAbortable: IAsyncDisposableAbortable
    {
        private readonly IAsyncDisposableAbortable m_Origin;
        
        public NullSafeAsyncDisposableAbortable(IAsyncDisposableAbortable origin)
        {
            m_Origin = origin;
        }

        public UniTask DisposeAsync()
        {
            return m_Origin?.DisposeAsync() ?? UniTask.CompletedTask;
        }

        public bool isAborted => m_Origin?.isAborted ?? true;
        public void Abort()
        {
            m_Origin?.Abort();
        }

        public DisposableAction AddAbortedListenerOnce(Action action)
        {
            if (m_Origin == null)
            {
                action();
                return DisposableAction.Empty;
            }
            return m_Origin.AddAbortedListenerOnce(action);
        }

        public bool isDisposedAsync => m_Origin?.isDisposedAsync ?? true;
    }
}