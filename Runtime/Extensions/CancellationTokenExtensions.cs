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
        
        /// <summary>
        /// Be careful, this method is not recommended to use if this token may never be canceled. Otherwise, it will cause a memory leak.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static UniTask ToCompletionUniTask(this CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested || !cancellationToken.CanBeCanceled)
            {
                return UniTask.CompletedTask;
            }
            
            var tsc = AutoResetUniTaskCompletionSource.Create();
            
            cancellationToken.Register(state =>
            {
                var source = (AutoResetUniTaskCompletionSource)state;
                source.TrySetResult();
            }, tsc);
            
            return tsc.Task;
        }
        
        public static CancellationSignal ToCancellationSignal(this CancellationToken cancellationToken)
        {
            return new CancellationSignal(cancellationToken);
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