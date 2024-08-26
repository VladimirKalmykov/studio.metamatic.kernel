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
        public IEnumerator MultipleClientsTest() => UniTask.ToCoroutine(async () =>
        {
            using var pool = new ParamLessPool<MyClass>();
            var obj1 = pool.Get();
            Assert.AreEqual(0, obj1.Version);
            Assert.AreEqual(1, pool.OutOfPoolCount);
            Assert.AreEqual(0, pool.Count);
            pool.Return(obj1);
            Assert.AreEqual(1, obj1.Version);
            Assert.AreEqual(0, pool.OutOfPoolCount);
            Assert.AreEqual(1, pool.Count);
        });
    }
}