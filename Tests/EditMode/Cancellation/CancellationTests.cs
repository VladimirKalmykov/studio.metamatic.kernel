using System.Collections;
using Metamatic;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Tests.EditMode.Cancellation
{
    [TestFixture]
    public class CancellationTests
    {
        [Test]
        public void SyncCancel()
        {
            using var cs = AsyncCancellationSource.Create();
            var cancellation = cs.Cancellation;
            cs.TryCancelSync();
            var signal = cancellation.Signal;
            Assert.IsTrue(signal.IsCancellationRequested);
        }
        
        [Test]
        public void AsyncCancel()
        {
            using var cs = AsyncCancellationSource.Create();
            var cancellation = cs.Cancellation;
            cs.TryCancelAsync();
            var signal = cs.Signal;
            Assert.IsTrue(signal.IsCancellationRequested, "Signal must be cancelled");
            Assert.IsTrue(cancellation.isCancelledAsync, "It should be cancelled async");
        }

        [UnityTest]
        public IEnumerator CancelAsyncCancel() => UniTask.ToCoroutine(async () =>
        {
            using var cs = AsyncCancellationSource.Create();
            var cancellation = cs.Cancellation;
            cs.TryCancelAsync();
            var signal = cs.Signal;
            Assert.IsTrue(signal.IsCancellationRequested, "Signal must be cancelled");
            Assert.IsTrue(cancellation.isCancelledAsync, "It should be cancelled async");
            var abortDisposingAsyncSignal = cancellation.AbortSignal;
            Assert.IsFalse(abortDisposingAsyncSignal.IsCancellationRequested, "Abort disposing signal should not be cancelled");
            Assert.IsTrue(cancellation.isAlive, "Cancellation source should be alive");
            await UniTask.Delay(100);
            cs.TryCancelSync();
            Assert.IsTrue(abortDisposingAsyncSignal.IsCancellationRequested, "Abort disposing signal should be cancelled");
            Assert.IsFalse(cancellation.isAlive, "Cancellation source should be returned to the pool");
        });
    }
}