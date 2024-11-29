using System;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    public interface ILoop : IDisposable
    {
        UniTask WaitUntil(Func<bool> action, CancellationSignal cancellationSignal = default);

        UniTask WaitWhile(Func<bool> action, CancellationSignal cancellationSignal = default);

        DisposableAction AddAction(Action action);

        UniTask Delay(TimeSpan timeSpan, CancellationSignal cancellationSignal = default);

        UniTask Delay(float seconds, CancellationSignal cancellationSignal = default);

        Loop.YieldAwaitable Yield();

        Loop.YieldAwaitable Yield(CancellationSignal signal);
    }
}