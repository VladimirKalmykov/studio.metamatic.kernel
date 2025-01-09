using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Metamatic;

namespace DevTools
{
    public struct StagnationCatcher
    {
        public static IDisposable Create(float timeout, Action onStagnation)
        {
            var cancellationToken = new CancellationTokenSource();
            UniTask.Delay(TimeSpan.FromSeconds(timeout), ignoreTimeScale: false, cancellationToken: cancellationToken.Token).ContinueWith(() =>
            {
                onStagnation?.Invoke();
            }).Forget();

            return DisposableAction.Create(() =>
            {
                cancellationToken.Cancel();
            });
        }
    }
}