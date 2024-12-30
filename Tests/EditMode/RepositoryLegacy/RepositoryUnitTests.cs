using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine.TestTools;
using Metamatic;
using Metamatic.StorageDriver;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
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
            var repository = new RepositoryLegacy();
            var storageA = new VersionControlStorageDriver(repository, new LocalMemoryStorageLegacy());
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
            var repository = new RepositoryLegacy();
            var storageA = new VersionControlStorageDriver(repository, new LocalMemoryStorageLegacy());
            var storageB = new VersionControlStorageDriver(repository, new LocalMemoryStorageLegacy());
            repository.AddStorage(storageA);
            repository.AddStorage(storageB);
        
            /* Add data to storage A */
            await storageA.Set<TestData>("key", new TestData
            {
                a = 1,
                b = "Hello",
            }, 1, default);
            var bestData = await repository.synchronizer.Sync<TestData>("key", GetDefaultTestData, Signal.None);
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
            var repository = new RepositoryLegacy();
            var storageA = new VersionControlStorageDriver(repository, new LocalMemoryStorageLegacy());
            var storageB = new VersionControlStorageDriver(repository, new LocalMemoryStorageLegacy());
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
            
            var bestData = await repository.synchronizer.Sync<TestData>("key", GetDefaultTestData, Signal.None);
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
            var repository = new RepositoryLegacy();
            var storageA = new VersionControlStorageDriver(repository, new LocalMemoryStorageLegacy());
            var storageB = new VersionControlStorageDriver(repository, new LocalMemoryStorageLegacy());
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
            
            var bestData = await repository.synchronizer.Sync<TestData>("key", GetDefaultTestData, Signal.None);
            /* Validate best data */
            Assert.AreEqual(16, bestData.data.a);
            Assert.AreEqual("Hello 1", bestData.data.b);
            
            /* Validate storage B */
            var (data, metadata, version) = await storageA.Get<TestData>("key", default);
            Assert.AreEqual(3, version);
            Assert.AreEqual(16, data.a);
            Assert.AreEqual("Hello 1", data.b);
        });
        
        [UnityTest]
        public IEnumerator TestСompetitionOfThreeStorages() => UniTask.ToCoroutine(async () =>
        {
            var repository = new RepositoryLegacy();
            var storageA = new VersionControlStorageDriver(repository, new LocalMemoryStorageLegacy());
            var storageB = new VersionControlStorageDriver(repository, new LocalMemoryStorageLegacy());
            var storageC = new VersionControlStorageDriver(repository, new LocalMemoryStorageLegacy());
            repository.AddStorage(storageA);
            repository.AddStorage(storageB);
            repository.AddStorage(storageC);
        
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
            }, 5, default);
            
            /* Add data to storage C */
            await storageC.Set<TestData>("key", new TestData
            {
                a = 18,
                b = "Hello 3",
            }, 2, default);
            
            var bestData = await repository.synchronizer.Sync<TestData>("key", GetDefaultTestData, Signal.None);
            /* Validate best data */
            Assert.AreEqual(17, bestData.data.a);
            Assert.AreEqual("Hello 2", bestData.data.b);
            
            /* Validate storage A */
            var (data, metadata, version) = await storageA.Get<TestData>("key", default);
            Assert.AreEqual(5, version);
            Assert.AreEqual(17, data.a);
            Assert.AreEqual("Hello 2", data.b);
            
            /* Validate storage B */
            (data, metadata, version) = await storageB.Get<TestData>("key", default);
            Assert.AreEqual(5, version);
            Assert.AreEqual(17, data.a);
            Assert.AreEqual("Hello 2", data.b);
        });
        
        [UnityTest]
        public IEnumerator NoValidDataFound() => UniTask.ToCoroutine(() =>
        {
            var repository = new RepositoryLegacy();
            var nativeStorageA = new LocalMemoryStorageLegacy();
            var nativeStorageB = new LocalMemoryStorageLegacy();
            repository.inMemoryStorage.storage.enabled = false;
            nativeStorageA.enabled = false;
            nativeStorageB.enabled = false;
            var storageA = new VersionControlStorageDriver(repository, nativeStorageA);
            var storageB = new VersionControlStorageDriver(repository, nativeStorageB);
            repository.AddStorage(storageA);
            repository.AddStorage(storageB);
        
            var exception = Assert.ThrowsAsync<StorageLegacy.UnreachableException>(async () =>
            {
                await repository.synchronizer.Sync("key", GetDefaultTestData, Signal.None);
            });
            Assert.AreEqual("Every driver returned error", exception.Message);
            return UniTask.CompletedTask;
        });
        
        [UnityTest]
        public IEnumerator TestNotFoundInAllStorages() => UniTask.ToCoroutine(async () =>
        {
            var repository = new RepositoryLegacy();
            var storageA = new VersionControlStorageDriver(repository, new LocalMemoryStorageLegacy());
            var storageB = new VersionControlStorageDriver(repository, new LocalMemoryStorageLegacy());
            repository.AddStorage(storageA);
            repository.AddStorage(storageB);
        
            var bestData = await repository.synchronizer.Sync<TestData>("key", GetDefaultTestData, Signal.None);
            /* Validate best data */
            Assert.AreEqual(30021, bestData.data.a);
            Assert.AreEqual("Default", bestData.data.b);
            /* Validate storage A */
            var (data, metadata, version) = await storageA.Get<TestData>("key", default);
            Assert.AreEqual(30021, data.a);
            Assert.AreEqual("Default", data.b);
            /* Validate storage B */
            (data, metadata, version) = await storageB.Get<TestData>("key", default);
            Assert.AreEqual(30021, data.a);
            Assert.AreEqual("Default", data.b);
        });
        
        [UnityTest]
        public IEnumerator TestDeleteFromStorage() => UniTask.ToCoroutine(async () =>
        {
            var repository = new RepositoryLegacy();
            var storageA = new VersionControlStorageDriver(repository, new LocalMemoryStorageLegacy());
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
            /* Delete data from storage A */
            await storageA.Delete<TestData>("key", default);
            var exception = Assert.ThrowsAsync<StorageLegacy.NotFoundException>(async () =>
            {
                await storageA.Get<TestData>("key", default);
            });
            Assert.AreEqual("Key not found", exception.Message);
        });
        
        [UnityTest]
        public IEnumerator TestTimeoutAndRetry() => UniTask.ToCoroutine(async () =>
        {
            var repository = new RepositoryLegacy();
            var mockStorage = new MockStorage();
            var storageA = new VersionControlStorageDriver(repository, mockStorage);
            repository.AddStorage(storageA);
            repository.timeout = 1f;
            repository.maxRetryCount = 2;
            mockStorage.latency = 2f;

            var requestsCount = 0;
            mockStorage.OnSetRequest += (type, metadataType, address) =>
            {
                requestsCount++;
            };

            Exception exception = default;
            try
            {
                await storageA.Set<TestData>("key", new TestData
                {
                    a = 1,
                    b = "Hello",
                }, 1, default);
            }
            catch (Exception e)
            {
                exception = e;
            }

            Assert.AreEqual("Request timeout", exception?.Message);
            Assert.AreEqual(3, requestsCount);
        });

        [UnityTest]
        public IEnumerator TestDisablingStorageOnError() => UniTask.ToCoroutine(async () =>
        {
            var repository = new RepositoryLegacy();
            var mockStorage = new MockStorage();
            var driver = new VersionControlStorageDriver(repository, mockStorage);

            mockStorage.getException = new Exception("Mock exception");
            repository.disableStorageDuration = 5f;
            
            Exception exception = default;
            try
            {
                await driver.Get<TestData>("key", CancellationToken.None);
            }
            catch (Exception e)
            {
                exception = e;
            }
            
            Assert.AreEqual("Mock exception", exception?.Message);
            Assert.AreEqual(false, mockStorage.enabled, "Storage must be disabled");
            
            /* Wait 6 seconds */
            await Task.Delay(TimeSpan.FromSeconds(6f)).ConfigureAwait(true);
            
            Assert.AreEqual(true, mockStorage.enabled, "Storage must be enabled again");
        });
        
        private UniTask<TestData> GetDefaultTestData(string address, Metamatic.Signal signal)
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