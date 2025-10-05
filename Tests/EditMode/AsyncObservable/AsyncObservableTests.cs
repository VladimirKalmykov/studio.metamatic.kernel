using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Metamatic.Tests
{
    internal sealed class TestObservable<T> : IObservableValue<T>
    {
        private T _value;
        public T value => _value;

        public TestObservable(T initial) { _value = initial; }

        public void Set(T v)
        {
            _value = v;
            for (int i = 0; i < _subs.Count; i++)
                _subs[i]?.Invoke(_value);
        }

        private readonly System.Collections.Generic.List<Action<T>> _subs = new();
        public DisposableAction Watch(Action<T> handler, bool skipInitial = false, Func<T, bool> validator = null)
        {
            if (!skipInitial)
            {
                if (validator == null || validator(_value))
                    handler?.Invoke(_value);
            }
            var idx = _subs.Count;
            _subs.Add(handler);
            var disposed = 0;
            return new DisposableAction(() =>
            {
                if (System.Threading.Interlocked.Exchange(ref disposed, 1) == 1) return;
                if ((uint)idx >= (uint)_subs.Count) return;
                _subs[idx] = null;
            });
        }

        public IObservableValue<TOut> Select<TOut>(Func<T, TOut> selector, bool compare, Func<TOut, TOut, bool> comparator = null)
        {
            return new SelectedObservable<T, TOut>(this, selector, compare, comparator);
        }
    }

    [TestFixture]
    public class AsyncObservableTests
    {
        [UnityTest]
        public IEnumerator SyncChain_AppliesInOrder() => UniTask.ToCoroutine(async () =>
        {
            var ao = AsyncObservable<int>.Create(1);
            ao.AddTransform(x => x + 1);
            ao.AddTransform(x => x * 10);
            await UniTask.Delay(1);
            Assert.AreEqual(20, ao.value);
            ao.Dispose();
        });

        [UnityTest]
        public IEnumerator AsyncTransform_CancelsOnRestart() => UniTask.ToCoroutine(async () =>
        {
            var src = new TestObservable<int>(5);
            var ao = AsyncObservable<int>.From(src);

            ao.AddTransform(async (x, s) =>
            {
                await UniTask.DelayFrame(10, PlayerLoopTiming.Update, s);
                return x + 100;
            });

            ao.AddTransform(x => x * 2);

            await UniTask.Delay(1);
            Assert.AreEqual(10, ao.value);

            src.Set(7);
            await UniTask.DelayFrame(2);
            Assert.AreEqual(14, ao.value);

            ao.Dispose();
        });

        [UnityTest]
        public IEnumerator DependentSyncTransform_ReactsToDep() => UniTask.ToCoroutine(async () =>
        {
            var dep = new TestObservable<int>(3);
            var ao = AsyncObservable<int>.Create(2);
            ao.AddTransform(dep, (cur, d) => cur + d);
            await UniTask.Delay(1);
            Assert.AreEqual(5, ao.value);

            dep.Set(10);
            await UniTask.Delay(1);
            Assert.AreEqual(12, ao.value);

            ao.Dispose();
        });

        [UnityTest]
        public IEnumerator DependentAsyncTransform_ReactsAndCancels() => UniTask.ToCoroutine(async () =>
        {
            var dep = new TestObservable<int>(1);
            var ao = AsyncObservable<int>.Create(10);

            ao.AddTransform(dep, async (cur, d, s) =>
            {
                await UniTask.DelayFrame(5, PlayerLoopTiming.Update, s);
                return cur * d;
            });

            await UniTask.DelayFrame(6);
            Assert.AreEqual(10, ao.value);

            dep.Set(4);
            await UniTask.DelayFrame(6);
            Assert.AreEqual(40, ao.value);

            ao.Dispose();
        });

        [UnityTest]
        public IEnumerator Removal_KeepsOrderAndRecomputes() => UniTask.ToCoroutine(async () =>
        {
            var ao = AsyncObservable<int>.Create(1);

            var d1 = ao.AddTransform(x => x + 1);
            var d2 = ao.AddTransform(x => x * 2);

            await UniTask.Delay(1);
            Assert.AreEqual(4, ao.value);

            d1.Dispose();
            await UniTask.Delay(1);
            Assert.AreEqual(2, ao.value);

            ao.AddTransform(x => x + 5);
            await UniTask.Delay(1);
            Assert.AreEqual(7, ao.value);

            d2.Dispose();
            await UniTask.Delay(1);
            Assert.AreEqual(6, ao.value);

            ao.Dispose();
        });

        [UnityTest]
        public IEnumerator Watch_NotifiesOnChange() => UniTask.ToCoroutine(async () =>
        {
            var ao = AsyncObservable<int>.Create(5);
            var observed = -1;
            var w = ao.Watch(v => observed = v, skipInitial: false);
            await UniTask.Delay(1);
            Assert.AreEqual(5, observed);

            var dep = new TestObservable<int>(2);
            ao.AddTransform(dep, (cur, d) => cur + d);
            await UniTask.Delay(1);
            Assert.AreEqual(7, observed);

            dep.Set(10);
            await UniTask.Delay(1);
            Assert.AreEqual(15, observed);

            w.Dispose();
            ao.Dispose();
        });
    }
}
