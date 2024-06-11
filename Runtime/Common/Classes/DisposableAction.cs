using System;

namespace Metamatic
{
    // TODO: consider changing to struct
    public readonly struct DisposableAction : IDisposable
    {
        public static DisposableAction Create(Action action)
        {
            return new DisposableAction(action);
        }
        public static readonly DisposableAction Empty = new DisposableAction(null);
        private readonly Action _action;

        public DisposableAction(Action action)
        {
            _action = action;
        }

        public void Dispose()
        {
            _action?.Invoke();
        }
        
        public void Invoke()
        {
            _action?.Invoke();
        }

        // Позволяет использовать DisposableAction напрямую как Action
        public static implicit operator Action(DisposableAction disposableAction)
        {
            return disposableAction._action;
        }
    }
}