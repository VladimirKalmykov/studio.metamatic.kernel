using System.Threading.Tasks;

namespace Metamatic
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Threading;

    public static class CancellationTokenExtensions
    {
        public static CancellationTokenAwaiter GetAwaiter(this CancellationToken cancellationToken)
        {
            return new CancellationTokenAwaiter(cancellationToken);
        }

        public struct CancellationTokenAwaiter : INotifyCompletion
        {
            private readonly CancellationToken cancellationToken;

            public CancellationTokenAwaiter(CancellationToken cancellationToken)
            {
                this.cancellationToken = cancellationToken;
            }

            public bool IsCompleted => cancellationToken.IsCancellationRequested;

            public void OnCompleted(Action continuation) => cancellationToken.Register(continuation);

            public void GetResult()
            {
            }
        }
        
        public static Task AsTask(this CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).SetResult(true), tcs);
            return tcs.Task;
        }
        
        public static void CancelAndDispose(this CancellationTokenSource cts)
        {
            cts?.Cancel();
            cts?.Dispose();
        }
        
        public static CancellationTokenSource Renew(this CancellationTokenSource cts)
        {
            cts?.Cancel();
            cts?.Dispose();
            return new CancellationTokenSource();
        }
    }
}