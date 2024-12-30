using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;
using Metamatic;

namespace Tests.EditMode
{
    [TestFixture]
    public class AbortableOperationTests
    {
        [UnityTest]
        public IEnumerator AsyncDisposeAndAbort() => UniTask.ToCoroutine(async () =>
        {
            var stages = new List<bool>();
            {
                var operation = AbortableOperation.Call(async cancellation =>
                {
                    var signal = cancellation.Signal;
                    stages.Add(signal.IsCancellationRequested); // Should be false
                    await UniTask.Delay(50, cancellationToken: signal);
                    await signal;
                    stages.Add(signal.IsCancellationRequested); // Should be true
                    stages.Add(cancellation.isCancelledAsync); // Should be true
                    try
                    {
                        await UniTask.Delay(500, cancellationToken: cancellation.AbortSignal);
                    }
                    catch (OperationCanceledException)
                    {
                        stages.Add(true);
                    }
                });
                await UniTask.Delay(100);
                operation.DisposeAsync().Forget();
                await UniTask.Delay(100);
                operation.Dispose();
            }
            Assert.AreEqual(new List<bool> { false, true, true, true }, stages);
        });
        
        [UnityTest]
        public IEnumerator AsyncDisposeAndSyncDisposeByDependency() => UniTask.ToCoroutine(async () =>
        {
            var stages = new List<bool>();
            using var externalCancellationSource = AsyncCancellationSource.Create();
            var externalCancellation = externalCancellationSource.Cancellation;
            AbortAfter(200, externalCancellation).Forget();
            var withAbort = AbortableObserver.CreateProxy(externalCancellation);
            {
                await using var operation = withAbort(AbortableOperation.Call(async cancellation =>
                {
                    try
                    {
                        var signal = cancellation.Signal;
                        stages.Add(signal.IsCancellationRequested); // Should be false
                        await UniTask.Delay(500, cancellationToken: signal);
                        await signal;
                    }
                    catch (OperationCanceledException)
                    {
                        stages.Add(true); // Should be true
                    }
                    finally
                    {
                        stages.Add(cancellation.isAborted); // Should be false because await using should dispose the operation asynchronously
                        if (!cancellation.isAborted)
                        {
                            try
                            {
                                await UniTask.Delay(500, cancellationToken: cancellation.AbortSignal);
                            }
                            catch (OperationCanceledException)
                            {
                                stages.Add(true);
                            }
                        }
                    }
                }));
            }
            Assert.AreEqual(new List<bool> { false, true, false, true }, stages);
            
            async UniTaskVoid AbortAfter(int milliseconds, IAbortable abortable)
            {
                await UniTask.Delay(milliseconds);
                abortable.Abort();
            }
        });
    }
}