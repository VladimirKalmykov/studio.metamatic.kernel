using System;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    public interface IEventLoop<in T>
    where T: System.Enum
    {
        public UniTask WaitUntil(T eventType, Func<bool> action, CancellationSignal cancellationSignal);
        public UniTask WaitWhile(T eventType, Func<bool> action, CancellationSignal cancellationSignal);
        public DisposableAction AddAction(T eventType, Action action);
        public Loop.YieldAwaitable Yield(T eventType);
        public UniTask Delay(T eventType, float seconds, CancellationSignal cancellationSignal = default);
        public UniTask Delay(T eventType, TimeSpan timeSpan, CancellationSignal cancellationSignal = default);
        // ReSharper disable once InconsistentNaming
        public float time { get; }
    }
}