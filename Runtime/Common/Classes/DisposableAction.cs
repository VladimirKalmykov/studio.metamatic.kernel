using System;

namespace Metamatic
{
    // TODO: consider changing to struct
    public readonly struct DisposableAction : IDisposable
    {
        #if DEBUG
        private readonly MicroState<bool> _disposed;
        #endif
        public static DisposableAction Create(Action action)
        {
            return new DisposableAction(action);
        }
        public static readonly DisposableAction Empty = new DisposableAction(null);
        private readonly Action _action;

        public DisposableAction(Action action)
        {
            _action = action;
            #if DEBUG
            _disposed = new MicroState<bool>(false);
            #endif
        }

        public void Dispose()
        {
            #if DEBUG
            if (_disposed.value)
            {
                throw new ObjectDisposedException(nameof(DisposableAction));
            }
            _disposed.value = true;
            #endif
            _action?.Invoke();
        }
        
        public void Invoke()
        {
            Dispose();
        }

        // Позволяет использовать DisposableAction напрямую как Action
        public static implicit operator Action(DisposableAction disposableAction)
        {
#if DEBUG
            return disposableAction.Dispose;
#else
            return disposableAction._action;
#endif
        }
    }
}