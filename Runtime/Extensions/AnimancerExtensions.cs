#if ANIMANCER
using System;
using System.Threading;
using Animancer;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    public static class AnimancerExtensions
    {
        [Obsolete("Use WhenEnds with cancellation signal instead")]
        public static UniTask WhenEnds(this AnimancerState state, CancellationToken cancellationToken = default)
        {
            var isReversed = state.Speed < 0;
            var isLooping = state.Clip.isLooping;

            if (isLooping)
            {
                return UniTask.WaitWhile(() => state.IsPlaying, cancellationToken: cancellationToken);
            }
            var tcs = new UniTaskCompletionSource();
            cancellationToken.Register(() =>
            {
                tcs.TrySetCanceled();
                state.Events.EndEvent = default;
            });
            if (isReversed && state.NormalizedTime <= 0f)
            {
                tcs.TrySetResult();
                return tcs.Task;
            } else if (!isReversed && state.NormalizedTime >= 1f)
            {
                tcs.TrySetResult();
                return tcs.Task;
            }
            var checkFn = isReversed ? new Func<bool>(() => state.NormalizedTime > 0f) : new Func<bool>(() => state.NormalizedTime < 1f);
            return UniTask.WaitWhile(checkFn, cancellationToken: cancellationToken);
            /*state.Events.EndEvent = new AnimancerEvent(isReversed ? 0f : 1f, () =>
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                tcs.TrySetResult();
                state.Events.EndEvent = default;
            });*/
            return tcs.Task;
        }
        
        public static async UniTask WhenEnds(this AnimancerState state, Signal cancellationSignal = default)
        {
            cancellationSignal.ThrowIfRaised();
            var isReversed = state.Speed < 0;
            var isLooping = state.Clip.isLooping;

            if (isLooping)
            {
                await UniTask.WaitWhile(() => state.IsPlaying && !cancellationSignal.IsCancellationRequested);
                cancellationSignal.ThrowIfRaised();
            }

            if (isReversed && state.NormalizedTime <= 0f)
            {
                return;
            }
            else if (!isReversed && state.NormalizedTime >= 1f)
            {
                return;
            }

            var checkFn = isReversed
                ? new Func<bool>(() => state.NormalizedTime > 0f && !cancellationSignal.IsCancellationRequested)
                : new Func<bool>(() => state.NormalizedTime < 1f && !cancellationSignal.IsCancellationRequested);
            await UniTask.WaitWhile(checkFn);
            cancellationSignal.ThrowIfRaised();
        }
    }
}
#endif