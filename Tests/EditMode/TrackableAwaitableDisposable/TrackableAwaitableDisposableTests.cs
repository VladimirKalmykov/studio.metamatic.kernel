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
        
        [UnityTest]
        public IEnumerator ProgressTest() => UniTask.ToCoroutine(async () =>
        {
            using var operation = TrackableAwaitableDisposable.Create(async (complete, progressSource, signal) =>
            {
                var step1 = progressSource.CreateStep();
                var step2 = progressSource.CreateStep();
                var step3 = progressSource.CreateStep();
                
                await D(5);
                step1.Dispose();
                await D(5);
                step2.Dispose();
                await D(5);
                step3.Dispose();
                complete();
            }, Signal.None);
            
            var progresses = new List<(int, int)>();

            using var watchProgress = operation.progress.Watch((stepProgress) =>
            {
                progresses.Add((stepProgress.Pass, stepProgress.Total));
            });
            await operation;
            
            Assert.AreEqual(4, progresses.Count);
            Assert.AreEqual((0, 3), progresses[0]);
            Assert.AreEqual((1, 3), progresses[1]);
            Assert.AreEqual((2, 3), progresses[2]);
            Assert.AreEqual((3, 3), progresses[3]);
        });
        
        [UnityTest]
        public IEnumerator ComposedProgressTest() => UniTask.ToCoroutine(async () =>
        {
            using var operation = TrackableAwaitableDisposable.Create(async (complete, progressSource, signal) =>
            {
                var subOperation1 = TrackableAwaitableDisposable.Create(async (subComplete, subProgressSource, subSignal) =>
                {
                    var step1 = subProgressSource.CreateStep();
                    var step2 = subProgressSource.CreateStep();
                    var step3 = subProgressSource.CreateStep();
                
                    await D(2);
                    step1.Dispose();
                    await D(2);
                    step2.Dispose();
                    await D(2);
                    step3.Dispose();
                    subComplete();
                }, signal);
                var subOperation2 = TrackableAwaitableDisposable.Create(async (subComplete, subProgressSource, subSignal) =>
                {
                    var step1 = subProgressSource.CreateStep();
                    var step2 = subProgressSource.CreateStep();
                    var step3 = subProgressSource.CreateStep();
                
                    await D(8);
                    step1.Dispose();
                    await D(2);
                    step2.Dispose();
                    await D(2);
                    step3.Dispose();
                    subComplete();
                }, signal);
                
                using var bind1 = progressSource.Bind(subOperation1.progress);
                using var bind2 = progressSource.Bind(subOperation2.progress);

                await subOperation1;
                await subOperation2;
                
                complete();
            }, Signal.None);
            
            var progresses = new List<(int, int)>();

            using var watchProgress = operation.progress.Watch((stepProgress) =>
            {
                progresses.Add((stepProgress.Pass, stepProgress.Total));
            });
            await operation;
            
            Assert.AreEqual(7, progresses.Count);
            Assert.AreEqual((0, 6), progresses[0]);
            Assert.AreEqual((1, 6), progresses[1]);
            Assert.AreEqual((2, 6), progresses[2]);
            Assert.AreEqual((3, 6), progresses[3]);
            Assert.AreEqual((4, 6), progresses[4]);
            Assert.AreEqual((5, 6), progresses[5]);
            Assert.AreEqual((6, 6), progresses[6]);
        });
    }
}
