using System;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    public interface ILoop : IDisposable
    {
        UniTask WaitUntil(Func<bool> action, Signal cancellationSignal = default);

        UniTask WaitWhile(Func<bool> action, Signal cancellationSignal = default);

        DisposableAction AddAction(Action action);

        UniTask Delay(TimeSpan timeSpan, Signal cancellationSignal = default);

        UniTask Delay(float seconds, Signal cancellationSignal = default);

        Loop.YieldAwaitable Yield();

        Loop.YieldAwaitable Yield(Signal signal);
    }
}