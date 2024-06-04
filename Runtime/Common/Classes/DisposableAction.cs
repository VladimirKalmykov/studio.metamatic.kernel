using System;

namespace Metamatic
{
    // TODO: consider changing to struct
    public struct DisposableAction : IDisposable
    {
        public static DisposableAction Create(Action action)
        {
            return new DisposableAction(action);
        }
        public static readonly DisposableAction Empty = new DisposableAction(null);
        private Action _action;

        public DisposableAction(Action action)
        {
            _action = action;
        }

        public void Dispose()
        {
            _action?.Invoke();
            _action = null;
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

        // Позволяет использовать Action для создания DisposableAction
        public static implicit operator DisposableAction(Action action)
        {
            return new DisposableAction(action);
        }
    }
}