using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using Metamatic;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Tests.EditMode
{
    [TestFixture]
    public class ParamLessPoolTests
    {
        public class MyClass: IDisposable
        {
            public int Version;
            public void Dispose()
            {
                unchecked
                {
                    Version++;
                }
            }
        }
        [UnityTest]
        public IEnumerator SimpleUse() => UniTask.ToCoroutine(() =>
        {
            using var pool = new ParamLessPool<MyClass>();
            var obj1 = pool.Get();
            Assert.AreEqual(0, obj1.Version);
            Assert.AreEqual(1, pool.OutOfPoolCount);
            Assert.AreEqual(0, pool.InPoolCount);
            pool.Return(obj1);
            Assert.AreEqual(1, obj1.Version);
            Assert.AreEqual(0, pool.OutOfPoolCount);
            Assert.AreEqual(1, pool.InPoolCount);
            return UniTask.CompletedTask;
        });
        
        [UnityTest]
        public IEnumerator AutoShrinkPool() => UniTask.ToCoroutine(async () =>
        {
            ParamLessPool<MyClass>.DefaultAnalysisPeriod = 0.5f;
            ParamLessPool<MyClass>.DefaultShrinkingPeriod = 1f;
            var a = ParamLessPool<MyClass>.GetShared();
            var b = ParamLessPool<MyClass>.GetShared();
            var c = ParamLessPool<MyClass>.GetShared();
            await UniTask.Delay(500);
            var pool = ParamLessPool<MyClass>.DebugGetSharedPool();
            Assert.AreEqual(3, pool.OutOfPoolCount);
            
            ParamLessPool<MyClass>.ReturnShared(a);
            ParamLessPool<MyClass>.ReturnShared(b);
            ParamLessPool<MyClass>.ReturnShared(c);
            
            /* After return pool should contain 3 items */
            Assert.AreEqual(3, pool.InPoolCount);
            
            await UniTask.Delay(1200);
            
            /* After first shrinking period pool should contain still 3 items (because it is max value in the latest second ) */
            Assert.AreEqual(3, pool.InPoolCount);

            await UniTask.Delay(2000);
            /* But after second shrinking period pool should contain 0 items */
            
            Assert.AreEqual(0, pool.InPoolCount);
        });
    }
}