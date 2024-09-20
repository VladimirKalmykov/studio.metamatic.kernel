using System;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    public readonly struct AsyncDisposableFunc : IUniTaskAsyncDisposable
    {
#if DEBUG
        private readonly Observable<bool> _disposed;
#endif
        public static AsyncDisposableFunc Create(Func<UniTask> action)
        {
            return new AsyncDisposableFunc(action);
        }
        public static readonly DisposableAction Empty = new DisposableAction(null);
        private readonly Func<UniTask> _action;

        public AsyncDisposableFunc(Func<UniTask> action)
        {
            _action = action;
#if DEBUG
            _disposed = new Observable<bool>(false);
#endif
        }

        public void Dispose()
        {
#if DEBUG
            if (_disposed?.value ?? false)
            {
                throw new ObjectDisposedException(nameof(DisposableAction));
            }

            if (_disposed != null)
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

        public UniTask DisposeAsync()
        {
#if DEBUG
            if (_disposed?.value ?? false)
            {
                throw new ObjectDisposedException(nameof(DisposableAction));
            }

            if (_disposed != null)
            {
                _disposed.value = true;
            }
#endif
            return _action == null ? UniTask.CompletedTask : _action.Invoke();
        }

        // Позволяет использовать DisposableAction напрямую как Action
        public static implicit operator Func<UniTask>(AsyncDisposableFunc disposableFunc)
        {
#if DEBUG
            return disposableFunc.DisposeAsync;
#else
            return disposableFunc.DisposeAsync;
#endif
        }
    }
}