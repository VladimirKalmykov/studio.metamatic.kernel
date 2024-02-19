using Cysharp.Threading.Tasks;

namespace Metamatic
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading;
    using System.Threading.Tasks;
    
    /// <summary>
    /// Handle concurrent access to the same URI.
    /// </summary>
    public class UriLockerManager
    {
        private readonly ConcurrentDictionary<string, (SemaphoreSlim semaphore, int count)> lockers = new();
        /// <summary>
        /// Ensure that only one operation is performed on the same URI at the same time.
        /// </summary>
        /// <example>
        /// /* Assuming that lockerManager instance already has been created */
        /// using (await lockerManager.GetAsyncUriLocker(uri, token)) { /* Do something with the URI */ }
        /// </example>
        /// <param name="uri"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<IDisposable> GetAsyncUriLocker(string uri, CancellationToken cancellationToken)
        {
            var locker = lockers.GetOrAdd(uri, _ => (new SemaphoreSlim(1, 1), 0));
            Interlocked.Increment(ref locker.count);
            await locker.semaphore.WaitAsync(cancellationToken); // Используем cancellationToken здесь

            return new ReleaseLock(async () =>
            {
                if (Interlocked.Decrement(ref locker.count) == 0)
                {
                    if (lockers.TryRemove(uri, out var removedLocker))
                    {
                        removedLocker.semaphore.Dispose();
                    }
                }
                else
                {
                    locker.semaphore.Release();
                }
            }, uri, this);
        }

        private void RemoveLocker(string uri)
        {
            lockers.TryRemove(uri, out var _);
        }

        private class ReleaseLock : IDisposable, IAsyncDisposable
        {
            private readonly Func<Task> _releaseFunc;
            private readonly string _uri;
            private readonly UriLockerManager _manager;
            private bool _disposed;

            public ReleaseLock(Func<Task> releaseFunc, string uri, UriLockerManager manager)
            {
                _releaseFunc = releaseFunc;
                _uri = uri;
                _manager = manager;
            }

            public void Dispose()
            {
                _releaseFunc().ConfigureAwait(false).GetAwaiter().GetResult();
            }

            public event Action OnAsyncDisposeCompleted;
            async UniTask IAsyncDisposable.DisposeAsync()
            {
                if (!_disposed)
                {
                    _disposed = true;
                    await _releaseFunc();
                }
            }
        }
    }
}