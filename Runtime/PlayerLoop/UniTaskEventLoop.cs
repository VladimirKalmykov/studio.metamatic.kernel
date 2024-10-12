using System;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    public struct UniTaskEventLoop: IEventLoop<PlayerLoopTiming>
    {
        public UniTask WaitUntil(PlayerLoopTiming eventType, Func<bool> action, CancellationSignal cancellationSignal)
        {
            bool CancellationValidation()
            {
                if (cancellationSignal.IsCancellationRequested)
                {
                    return true;
                }

                return action();
            }
            return UniTask.WaitUntil(CancellationValidation);
        }

        public UniTask WaitWhile(PlayerLoopTiming eventType, Func<bool> action, CancellationSignal cancellationSignal)
        {
            bool CancellationValidation()
            {
                return !cancellationSignal.IsCancellationRequested && action();
            }
            return UniTask.WaitWhile(CancellationValidation);
        }

        public DisposableAction AddAction(PlayerLoopTiming eventType, Action action)
        {
            var disposed = false;
            PlayerLoopHelper.AddAction(eventType, new LoopItem(() => { action(); return disposed; }));
            return new DisposableAction(() => disposed = true);
        }

        public Loop.YieldAwaitable Yield(PlayerLoopTiming eventType)
        {
            return new Loop.YieldAwaitable(UniTask.Yield().GetAwaiter());
        }

        public UniTask Delay(PlayerLoopTiming eventType, float seconds, CancellationSignal cancellationSignal = default)
        {
            return UniTask.Delay(TimeSpan.FromSeconds(seconds), false, PlayerLoopTiming.Update, cancellationSignal);
        }

        public UniTask Delay(PlayerLoopTiming eventType, TimeSpan timeSpan, CancellationSignal cancellationSignal = default)
        {
            return UniTask.Delay(timeSpan, false, PlayerLoopTiming.Update, cancellationSignal);
        }

        public float time => UnityEngine.Time.time;
    }
}