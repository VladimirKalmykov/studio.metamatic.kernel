using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using UnityEngine.TestTools;
using Metamatic;
using Metamatic.Effects;
using Metamatic.StorageDriver;
using NUnit.Framework;

namespace Tests.EditMode
{
    public class RepositoryUnitTests
    {
        [Serializable]
        public struct TestData
        {
            public int a;
            public string b;
        }
        [UnityTest]
        public async UniTask CreateRepositoryAndAddStorages()
        {
            var repository = new Repository();
            var storageA = new VersionControlStorageDriver(repository, new LocalMemoryStorage());
            var storageB = new VersionControlStorageDriver(repository, new LocalMemoryStorage());
            repository.AddStorage(storageA);
            repository.AddStorage(storageB);
            
            /* Add data to storage A */
            await storageA.Set<TestData>("key", new TestData
            {
                a = 1,
                b = "Hello",
            }, 1, default);
            /* Read data from storage A */
            var (data, metadata, version) = await storageA.Get<TestData>("key", default);
            Assert.AreEqual(1, data.a);
            Assert.AreEqual("Hello", data.b);
        }
    }
}