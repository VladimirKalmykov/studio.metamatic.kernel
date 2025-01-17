using System;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    public interface IEventLoop<in T>
    where T: System.Enum
    {
        public UniTask WaitUntil(T eventType, Func<bool> action, Signal cancellationSignal);
        public UniTask WaitWhile(T eventType, Func<bool> action, Signal cancellationSignal);
        public DisposableAction AddAction(T eventType, Action action);
        public Loop.YieldAwaitable Yield(T eventType, Signal cancellationSignal = default);
        public Loop.YieldAwaitable Yield(Signal cancellationSignal);
        public UniTask Delay(T eventType, float seconds, Signal cancellationSignal = default);
        public UniTask Delay(T eventType, TimeSpan timeSpan, Signal cancellationSignal = default);
        // ReSharper disable once InconsistentNaming
        public float time { get; }
    }
}