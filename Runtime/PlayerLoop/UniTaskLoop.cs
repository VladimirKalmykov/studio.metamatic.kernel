using System;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    public readonly struct UniTaskLoop: ILoop
    {
        private readonly PlayerLoopTiming m_EventType;
        
        public UniTaskLoop(PlayerLoopTiming eventType)
        {
            m_EventType = eventType;
        }
        
        public void Dispose()
        {
            // Skip
        }

        public UniTask WaitUntil(Func<bool> predicate, Signal cancellationSignal = default)
        {
            return PlayerLoop.EachFrameUntil(predicate, m_EventType, cancellationSignal);
        }

        public UniTask WaitWhile(Func<bool> action, Signal cancellationSignal = default)
        {
            return PlayerLoop.EachFrameUntil(() => !action(), m_EventType, cancellationSignal);
        }

        public DisposableAction AddAction(Action action)
        {
            return PlayerLoop.EachFrame(action, m_EventType);
        }

        public UniTask Delay(TimeSpan timeSpan, Signal cancellationSignal = default)
        {
            return PlayerLoop.Delay(timeSpan, m_EventType, cancellationSignal);
        }

        public UniTask Delay(float seconds, Signal cancellationSignal = default)
        {
            return PlayerLoop.Delay(TimeSpan.FromSeconds(seconds), m_EventType, cancellationSignal);
        }

        public Loop.YieldAwaitable Yield()
        {
            return new Loop.YieldAwaitable(UniTask.Yield(m_EventType).GetAwaiter());
        }

        public Loop.YieldAwaitable Yield(Signal signal)
        {
            return new Loop.YieldAwaitable(UniTask.Yield(m_EventType).GetAwaiter(), signal);
        }
    }
}