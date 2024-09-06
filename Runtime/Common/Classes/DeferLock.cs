using System.Threading;
using Cysharp.Threading.Tasks;

namespace Metamatic
    {
        using System;
        using System.Runtime.CompilerServices;
        /// <summary>
        /// Lightweight synchronization primitive designed to facilitate deferred asynchronous operations. It can be locked and unlocked multiple times, allowing multiple awaiters to wait for the lock to be released. This class is particularly useful in scenarios where you need to control the flow of asynchronous operations, ensuring that certain operations are deferred until a specific condition is met.
        /// </summary>
        public class DeferLock : INotifyCompletion, IDisposable
        {
            private int _lockCount;
            private bool _isDisposed;
            public event Action OnUnlock;

            public DisposableAction Lock()
            {
                if (_isDisposed) throw new ObjectDisposedException(nameof(DeferLock));
                _lockCount++;
                return new DisposableAction(Unlock);
            }
            
            public async UniTask<DisposableAction> Lock(CancellationSignal signal) {
                await WhenUnlocked(signal);
                if (signal.IsCancellationRequested)
                {
                    return DisposableAction.Empty;
                }
                return Lock();
            }

            private void Unlock()
            {
                if (_isDisposed) throw new ObjectDisposedException(nameof(DeferLock));
                _lockCount--;

                if (_lockCount == 0)
                {
                    OnUnlock?.Invoke();
                    OnUnlock = null;
                }
            }

            public DeferLock GetAwaiter()
            {
                if (_isDisposed) throw new ObjectDisposedException(nameof(DeferLock));
                return this;
            }
            
            public UniTask WhenUnlocked(CancellationSignal cancellationSignal = default)
            {
                if (_isDisposed) throw new ObjectDisposedException(nameof(DeferLock));
                if (_lockCount == 0)
                {
                    return UniTask.CompletedTask;
                }

                var tcs = AutoResetUniTaskCompletionSource.Create();
                DisposableAction registration = default;

                registration = cancellationSignal.Register(() =>
                {
                    registration.Dispose();
                    OnUnlock -= Handler;
                    tcs.TrySetCanceled(cancellationSignal);
                });
                void Handler()
                {
                    registration.Dispose();
                    if (cancellationSignal.IsCancellationRequested)
                    {
                        tcs.TrySetCanceled(cancellationSignal);
                    }
                    else
                    {
                        tcs.TrySetResult();
                    }
                }
                OnUnlock += Handler;
                return tcs.Task;
            }

            public bool IsLocked => _lockCount > 0;

            public bool IsCompleted => _lockCount == 0;

            public void GetResult() { }

            public void OnCompleted(Action continuation)
            {
                if (_lockCount > 0)
                {
                    Action callbackWrapper = null;
                    callbackWrapper = () =>
                    {
                        OnUnlock -= callbackWrapper;
                        if (_isDisposed) throw new ObjectDisposedException(nameof(DeferLock));
                        continuation();
                    };
                    OnUnlock += callbackWrapper;
                }
                else
                {
                    continuation();
                }
            }
            
            public void ContinueAfterUnlock(Action continuation, CancellationToken cancellationToken = default)
            {
                if (_isDisposed) throw new ObjectDisposedException(nameof(DeferLock));
                if (_lockCount > 0)
                {
                    OnUnlock += () =>
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            return;
                        }
                        continuation();
                    };
                }
                else
                {
                    continuation();
                }
            }
            
            /// <summary>
            /// Use Reset instead of Dispose to reset the lock state. Probably in the future, we will change Dispose logic to actually dispose the object.
            /// </summary>
            public void Reset()
            {
                _isDisposed = true;
                _lockCount = 0;
                
                OnUnlock?.Invoke();
                OnUnlock = null;
            }

            public void Dispose()
            {
                _isDisposed = true;
                _lockCount = 0;
                
                OnUnlock?.Invoke();
                OnUnlock = null;
            }
            
            /* Support casting to bool */
            public static implicit operator bool(DeferLock d) => d.IsLocked;
        }
    }