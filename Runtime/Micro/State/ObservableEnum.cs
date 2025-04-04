using System;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    /// <summary>
    /// Observable that can be used to watch the changes of the enum flags value.
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    public class ObservableEnum<TValue>: Observable<TValue> where TValue: Enum
    {
        private enum MaskMatchStatus
        {
            Idle,
            Matched,
            NotMatched
        }

        /// <summary>
        /// Watches the observable for the changes and triggers the action when the mask is matched.
        /// </summary>
        /// <param name="mask"></param>
        /// <param name="action"></param>
        /// <param name="onlyMatched">Call handler only if the value matched to mask</param>
        /// <returns></returns>
        public DisposableAction WatchAsync(TValue mask, Func<TValue, Signal, UniTaskVoid> action, bool onlyMatched = false)
        {
            var previouslyMatched = MaskMatchStatus.Idle;
            SignalSource cancellationSignalSource = default;
            var watcher = Watch((nextValue) =>
            {
                var nextMatched = nextValue.HasFlag(mask) ? MaskMatchStatus.Matched : MaskMatchStatus.NotMatched;
                if (nextMatched != previouslyMatched)
                {
                    previouslyMatched = nextMatched;
                    cancellationSignalSource.TryRaise();
                    if (nextMatched == MaskMatchStatus.Matched || !onlyMatched)
                    {
                        cancellationSignalSource = SignalSource.Create();
                        action(nextValue, cancellationSignalSource.Signal).Forget();
                    }
                }
            });
            
            return DisposableAction.Create(() =>
            {
                watcher.Dispose();
                cancellationSignalSource.TryRaise();
            });
        }
        
        /// <summary>
        /// Watches the observable for the changes and triggers the action when the value is matched to any of the masks.
        /// </summary>
        /// <param name="mask"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public DisposableAction WatchDiffAsync(TValue mask, Func<TValue, Signal, UniTaskVoid> action)
        {
            ulong previousMatched = default;
            ulong maskUlong = Convert.ToUInt64(mask);
            SignalSource cancellationSignalSource = default;
            var watcher = Watch((nextValue) =>
            {
                var nextMatched = Convert.ToUInt64(nextValue) & maskUlong;
                if (nextMatched != previousMatched)
                {
                    previousMatched = nextMatched;
                    cancellationSignalSource.TryRaise();
                    cancellationSignalSource = SignalSource.Create();
                    action(nextValue, cancellationSignalSource.Signal).Forget();
                }
            });
            
            return DisposableAction.Create(() =>
            {
                watcher.Dispose();
                cancellationSignalSource.TryRaise();
            });
        }
        
        public bool HasFlag(TValue flag)
        {
            return Value.HasFlag(flag);
        }
    }
}