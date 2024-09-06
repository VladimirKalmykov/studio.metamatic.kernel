using System;

namespace Metamatic
{
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
            if (_disposed?.value ?? false)
            {
                throw new ObjectDisposedException(nameof(DisposableAction));
            }

            if (_action != null && _disposed != null)
            {
                _disposed.value = true;
            }
            #endif
            _action?.Invoke();
        }
        
        public void Invoke()
        {
            Dispose();
        }
        
        public override int GetHashCode()
        {
            return _action.GetHashCode();
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