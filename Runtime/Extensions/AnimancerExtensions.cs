#if ANIMANCER
using System;
using System.Threading;
using Animancer;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    public static class AnimancerExtensions
    {
        /// <summary>
        /// TODO: Remove DebugID after testing
        /// </summary>
        /// <param name="state"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="debugID"></param>
        /// <returns></returns>
        public static UniTask WhenEnds(this AnimancerState state, CancellationToken cancellationToken = default, string debugID = "")
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
        
        public static async UniTask WhenEndsOrChanged(this AnimancerState state, CancellationToken cancellationToken = default, string debugID = "")
        {
            var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var currentClip = state.Clip;
            var waitClipChanged = UniTask.WaitWhile(() =>
            {
                return state.Clip == currentClip;
            }, cancellationToken: cancellationTokenSource.Token);
            await UniTask.WhenAny(waitClipChanged, state.WhenEnds(cancellationTokenSource.Token, debugID));
            cancellationTokenSource.CancelAndDispose();
        }
    }
}
#endif