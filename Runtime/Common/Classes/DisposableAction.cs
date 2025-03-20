using System;

namespace Metamatic
{
    public readonly struct DisposableAction : IDisposable
    {
        #if DEBUG
        public readonly Observable<bool> Disposed;
        #endif
        public static DisposableAction Create(Action action)
        {
            return new DisposableAction(action);
        }
        public static readonly DisposableAction Empty = new DisposableAction(null);
        public bool isEmpty => _action == null;
        private readonly Action _action;

        public DisposableAction(Action action)
        {
            _action = action;
            #if DEBUG
            Disposed = new Observable<bool>(false);
            #endif
        }

        public void Dispose()
        {
            #if DEBUG
            if (Disposed?.value ?? false)
            {
                throw new ObjectDisposedException(nameof(DisposableAction));
            }

            if (_action != null && Disposed != null)
            {
                Disposed.value = true;
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
    
    public readonly struct DisposableAction<T> : IDisposable
    {
#if DEBUG
        public readonly Observable<bool> Disposed;
#endif
        public static DisposableAction<T> Create<T>(Action<T> action, T payload)
        {
            return new DisposableAction<T>(action, payload);
        }
        public static readonly DisposableAction Empty = new DisposableAction(null);
        public bool isEmpty => _action == null;
        private readonly Action<T> _action;
        private readonly T _payload;

        public DisposableAction(Action<T> action, T payload)
        {
            _action = action;
            _payload = payload;
#if DEBUG
            Disposed = new Observable<bool>(false);
#endif
        }

        public void Dispose()
        {
#if DEBUG
            if (Disposed?.value ?? false)
            {
                throw new ObjectDisposedException(nameof(DisposableAction));
            }

            if (_action != null && Disposed != null)
            {
                Disposed.value = true;
            }
#endif
            _action?.Invoke(_payload);
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
        public static implicit operator Action(DisposableAction<T> disposableAction)
        {
            return disposableAction.Dispose;
        }
    }
}