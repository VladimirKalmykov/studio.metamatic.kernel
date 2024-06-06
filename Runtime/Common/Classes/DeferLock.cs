namespace Metamatic
    {
        using System;
        using System.Collections.Concurrent;
        using System.Runtime.CompilerServices;
        /// <summary>
        /// Lightweight synchronization primitive designed to facilitate deferred asynchronous operations. It can be locked and unlocked multiple times, allowing multiple awaiters to wait for the lock to be released. This class is particularly useful in scenarios where you need to control the flow of asynchronous operations, ensuring that certain operations are deferred until a specific condition is met.
        /// </summary>
        public class DeferLock : INotifyCompletion, IDisposable
        {
            private int _lockCount;
            private bool _isDisposed;
            private readonly ConcurrentQueue<Action> _waiters = new ConcurrentQueue<Action>();

            public DeferLock()
            {
                _lockCount = 0;
                _isDisposed = false;
            }

            public DisposableAction Lock()
            {
                if (_isDisposed) throw new ObjectDisposedException(nameof(DeferLock));
                _lockCount++;
                return new DisposableAction(Unlock);
            }

            private void Unlock()
            {
                if (_isDisposed) throw new ObjectDisposedException(nameof(DeferLock));
                _lockCount--;

                if (_lockCount == 0)
                {
                    while (_waiters.TryDequeue(out var waiter))
                    {
                        waiter();
                    }
                }
            }

            public DeferLock GetAwaiter()
            {
                if (_isDisposed) throw new ObjectDisposedException(nameof(DeferLock));
                return this;
            }

            public bool IsLocked => _lockCount > 0;

            public bool IsCompleted => _lockCount == 0;

            public void GetResult() { }

            public void OnCompleted(Action continuation)
            {
                if (_lockCount > 0)
                {
                    _waiters.Enqueue(continuation);
                }
                else
                {
                    continuation();
                }
            }

            public void Dispose()
            {
                _isDisposed = true;
                _lockCount = 0;

                while (_waiters.TryDequeue(out var waiter))
                {
                    waiter();
                }
            }
        }
    }