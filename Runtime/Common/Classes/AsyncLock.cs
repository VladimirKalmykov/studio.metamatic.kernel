using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    public class AsyncLock
    {
        private readonly SemaphoreSlim semaphore = new(1, 1);

        public async UniTask<IDisposable> LockAsync(CancellationToken cancellationToken = default)
        {
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            return new LockReleaser(semaphore);
        }

        public async UniTask<IDisposable> LockAsync(Signal cancellationSignal = default)
        {
            await semaphore.WaitAsync(cancellationSignal).ConfigureAwait(false);
            return new LockReleaser(semaphore);
        }

        private readonly struct LockReleaser : IDisposable
        {
            private readonly SemaphoreSlim semaphore;

            public LockReleaser(SemaphoreSlim semaphore) => this.semaphore = semaphore;

            public void Dispose() => semaphore?.Release();
        }
    }
}