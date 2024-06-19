using System;
using System.Collections;
using System.Threading;
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
        public IEnumerator TestSimpleWriteToStorage() => UniTask.ToCoroutine(async () =>
        {
            var repository = new Repository();
            var storageA = new VersionControlStorageDriver(repository, new LocalMemoryStorage());
            repository.AddStorage(storageA);
        
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
            Assert.AreEqual(1, version);
        });
        
        [UnityTest]
        public IEnumerator TestSimpleSyncWithTwoStorages() => UniTask.ToCoroutine(async () =>
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
            var bestData = await repository.synchronizer.Sync<TestData>("key", GetDefaultTestData, CancellationToken.None);
            /* Validate best data */
            Assert.AreEqual(1, bestData.data.a);
            Assert.AreEqual("Hello", bestData.data.b);
            
            /* Validate storage B */
            var (data, metadata, version) = await storageB.Get<TestData>("key", default);
            Assert.AreEqual(1, version);
            Assert.AreEqual(1, data.a);
            Assert.AreEqual("Hello", data.b);
        });
        
        [UnityTest]
        public IEnumerator TestСompetitionOfTwoStorages() => UniTask.ToCoroutine(async () =>
        {
            var repository = new Repository();
            var storageA = new VersionControlStorageDriver(repository, new LocalMemoryStorage());
            var storageB = new VersionControlStorageDriver(repository, new LocalMemoryStorage());
            repository.AddStorage(storageA);
            repository.AddStorage(storageB);
        
            /* Add data to storage A */
            await storageA.Set<TestData>("key", new TestData
            {
                a = 16,
                b = "Hello 1",
            }, 3, default);
            
            /* Add data to storage B */
            await storageB.Set<TestData>("key", new TestData
            {
                a = 17,
                b = "Hello 2",
            }, 4, default);
            
            var bestData = await repository.synchronizer.Sync<TestData>("key", GetDefaultTestData, CancellationToken.None);
            /* Validate best data */
            Assert.AreEqual(17, bestData.data.a);
            Assert.AreEqual("Hello 2", bestData.data.b);
            
            /* Validate storage B */
            var (data, metadata, version) = await storageA.Get<TestData>("key", default);
            Assert.AreEqual(4, version);
            Assert.AreEqual(17, data.a);
            Assert.AreEqual("Hello 2", data.b);
        });
        
        [UnityTest]
        public IEnumerator TestTwoStoragesHasEqualVersion() => UniTask.ToCoroutine(async () =>
        {
            var repository = new Repository();
            var storageA = new VersionControlStorageDriver(repository, new LocalMemoryStorage());
            var storageB = new VersionControlStorageDriver(repository, new LocalMemoryStorage());
            repository.AddStorage(storageA);
            repository.AddStorage(storageB);
        
            /* Add data to storage A */
            await storageA.Set<TestData>("key", new TestData
            {
                a = 16,
                b = "Hello 1",
            }, 3, default);
            
            /* Add data to storage B */
            await storageB.Set<TestData>("key", new TestData
            {
                a = 16,
                b = "Hello 1",
            }, 3, default);
            
            var bestData = await repository.synchronizer.Sync<TestData>("key", GetDefaultTestData, CancellationToken.None);
            /* Validate best data */
            Assert.AreEqual(16, bestData.data.a);
            Assert.AreEqual("Hello 1", bestData.data.b);
            
            /* Validate storage B */
            var (data, metadata, version) = await storageA.Get<TestData>("key", default);
            Assert.AreEqual(3, version);
            Assert.AreEqual(16, data.a);
            Assert.AreEqual("Hello 1", data.b);
        });

        private UniTask<TestData> GetDefaultTestData(string address, CancellationToken token)
        {
            var data = new TestData
            {
                a = 30021,
                b = "Default",
            };
            return UniTask.FromResult(data);
        }
    }
}