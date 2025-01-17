using System;
using System.Runtime.CompilerServices;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    public class UniTaskEventLoop: IEventLoop<PlayerLoopTiming>
    {
        public static readonly UniTaskEventLoop Shared = new UniTaskEventLoop();
        public UniTask WaitUntil(PlayerLoopTiming eventType, Func<bool> action, Signal cancellationSignal)
        {
            return UniTask.WaitUntil(CancellationValidation);

            bool CancellationValidation()
            {
                if (cancellationSignal.IsCancellationRequested)
                {
                    return true;
                }

                return action();
            }
        }

        public UniTask WaitWhile(PlayerLoopTiming eventType, Func<bool> action, Signal cancellationSignal)
        {
            return UniTask.WaitWhile(CancellationValidation);

            bool CancellationValidation()
            {
                return !cancellationSignal.IsCancellationRequested && action();
            }
        }

        public DisposableAction AddAction(PlayerLoopTiming eventType, Action action)
        {
            var disposed = false;
            PlayerLoopHelper.AddAction(eventType, new LoopItem(() => { action(); return disposed; }));
            return new DisposableAction(() => disposed = true);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Loop.YieldAwaitable Yield(PlayerLoopTiming eventType, Signal cancellationSignal = default)
        {
            return new Loop.YieldAwaitable(UniTask.Yield(eventType).GetAwaiter(), cancellationSignal);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Loop.YieldAwaitable Yield(Signal cancellationSignal)
        {
            return new Loop.YieldAwaitable(UniTask.Yield(PlayerLoopTiming.Update).GetAwaiter(), cancellationSignal);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UniTask Delay(PlayerLoopTiming eventType, float seconds, Signal cancellationSignal = default)
        {
            return UniTask.Delay(TimeSpan.FromSeconds(seconds), false, PlayerLoopTiming.Update, cancellationSignal);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UniTask Delay(PlayerLoopTiming eventType, TimeSpan timeSpan, Signal cancellationSignal = default)
        {
            return UniTask.Delay(timeSpan, false, PlayerLoopTiming.Update, cancellationSignal);
        }

        public float time => UnityEngine.Time.time;
    }
}