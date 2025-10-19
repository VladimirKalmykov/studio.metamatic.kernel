using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;
using Metamatic;

namespace Metamatic.Tests
{
    public class TrackableAwaitableDisposableTests
    {
        private static UniTask D(int ms) => UniTask.Delay(ms, ignoreTimeScale: true);

        private DisposableAction WatchProgress(Progress progress, Action<StepProgress> onProgress)
        {
            var signalSource = SignalSource.Create();
            var signal = signalSource.Signal;
            var currentValue = progress.value;
            UniTask.Create(async () =>
            {
                while (!signal.isRaised)
                {
                    var nextValue = progress.value;
                    if (nextValue.Pass != currentValue.Pass || nextValue.Total != currentValue.Total)
                    {
                        currentValue = nextValue;
                        onProgress(nextValue);
                    }
                    await D(1);
                }
            }).Forget();

            return DisposableAction.Create(signalSource.TryRaise);
        }

        [UnityTest]
        public IEnumerator 
            ProgressTest() => UniTask.ToCoroutine(async () =>
        {
            var actionHistory = new List<int>();
            /*
             * 1 - pending is started
             * 2 - pending is stopped
             */
            using var operation = TrackableAwaitableDisposable.Create(async (complete, progressSource, signal) =>
            {
                var step1 = progressSource.CreateStep();
                var step2 = progressSource.CreateStep();
                

                await D(100);
                step1.Dispose();
                await D(100);
                step2.Dispose();
                
                
                var step3 = progressSource.CreateStep();
                await D(100);
                step3.Dispose();
                await D(100);
                complete();
            }, Signal.None);

            var progresses = new List<StepProgress>();

            {
                using var watchPending = operation.progress.WatchPendingAsync(async (pendingCancellationSignal) =>
                {
                    actionHistory.Add(1);
                    progresses.Add(operation.progress.State);
                    await pendingCancellationSignal;
                    progresses.Add(operation.progress.State);
                    actionHistory.Add(2);
                });

                await operation;
            }
            await D(25);
            
            // Action history
            Assert.AreEqual(4, actionHistory.Count);
            Assert.AreEqual(1, actionHistory[0]);
            Assert.AreEqual(2, actionHistory[1]);
            Assert.AreEqual(1, actionHistory[2]);
            Assert.AreEqual(2, actionHistory[3]);
            
            // Progress history
            Assert.AreEqual(4, progresses.Count);
            Assert.AreEqual((0, 2), (progresses[0].Pass, progresses[0].Total));
            Assert.AreEqual((2, 2), (progresses[1].Pass, progresses[1].Total));
            Assert.AreEqual((0, 1), (progresses[2].Pass, progresses[2].Total));
            Assert.AreEqual((1, 1), (progresses[3].Pass, progresses[2].Total));
        });

        [UnityTest]
        public IEnumerator ComposedProgressTest() => UniTask.ToCoroutine(async () =>
        {
            var actionHistory = new List<int>();
            using var operation = TrackableAwaitableDisposable.Create(async (complete, progressSource, signal) =>
            {
                var subOperation1 = TrackableAwaitableDisposable.Create(
                    async (subComplete, subProgressSource, subSignal) =>
                    {
                        var step1 = subProgressSource.CreateStep();

                        await D(100);
                        step1.Dispose();
                        subComplete();
                    }, signal);
                var subOperation2 = TrackableAwaitableDisposable.Create(
                    async (subComplete, subProgressSource, subSignal) =>
                    {
                        var step1 = subProgressSource.CreateStep();
                        var step2 = subProgressSource.CreateStep();
                        var step3 = subProgressSource.CreateStep();

                        await D(100);
                        step1.Dispose();
                        await D(100);
                        step2.Dispose();
                        await D(100);
                        step3.Dispose();
                        subComplete();
                    }, signal);

                using var bind1 = progressSource.Bind(subOperation1.progress);
                using var bind2 = progressSource.Bind(subOperation2.progress);

                await subOperation1;
                await subOperation2;

                complete();
            }, Signal.None);

            var progresses = new List<StepProgress>();

            {
                using var watchPending = operation.progress.WatchPendingAsync(async (pendingCancellationSignal) =>
                {
                    actionHistory.Add(1);
                    progresses.Add(operation.progress.State);
                    await pendingCancellationSignal;
                    progresses.Add(operation.progress.State);
                    actionHistory.Add(2);
                });

                await operation;
            }

            // Action history
            Assert.AreEqual(2, actionHistory.Count);
            Assert.AreEqual(1, actionHistory[0]);
            Assert.AreEqual(2, actionHistory[1]);
            
            // Progress history
            Assert.AreEqual(2, progresses.Count);
            Assert.AreEqual((0, 4), (progresses[0].Pass, progresses[0].Total));
            Assert.AreEqual((4, 4), (progresses[1].Pass, progresses[1].Total));
        });
    }
}