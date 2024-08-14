using System.Threading.Tasks;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Threading;

    public static class CancellationTokenExtensions
    {
        public static CancellationTokenAwaitable.Awaiter GetAwaiter(this CancellationToken cancellationToken)
        {
            return new CancellationTokenAwaitable.Awaiter(cancellationToken);
        }
        
        [Obsolete("Use CancellationTokenAwaitable.Awaiter instead")]
        public struct CancellationTokenAwaiter : INotifyCompletion, ICriticalNotifyCompletion
        {
            private readonly CancellationToken cancellationToken;

            public CancellationTokenAwaiter(CancellationToken cancellationToken)
            {
                this.cancellationToken = cancellationToken;
            }

            public bool IsCompleted => cancellationToken.IsCancellationRequested;

            public void OnCompleted(Action continuation) => cancellationToken.Register(continuation);
            public void UnsafeOnCompleted(Action continuation)
            {
                throw new NotImplementedException();
            }

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