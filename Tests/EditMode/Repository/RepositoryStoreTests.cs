using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using Metamatic.StorageDriver;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Metamatic.Tests.EditMode
{
    [TestFixture]
    public class RepositoryStoreTests
    {
        struct TestData
        {
            public int id;
            public string name;
        }
        private UniTask<TestData> GetTestData(string address, CancellationToken token)
        {
            return UniTask.FromResult(new TestData {id = 33, name = "default"});
        }
        [UnityTest]
        public IEnumerator TestCreateRepositoryStore() => UniTask.ToCoroutine(async () =>
        {
            var repository = new Repository();
            var storage = new LocalMemoryStorage();
            var driver = new VersionControlStorageDriver(repository, storage);
            repository.AddStorage(driver);
            
            await using var store = await repository.CreateStore("test", GetTestData, CancellationToken.None);
            Assert.IsNotNull(store);
            Assert.AreEqual(33, store.value.id);
            Assert.AreEqual("default", store.value.name);
        });
        
        [UnityTest]
        public IEnumerator TestCreateAndGetStore() => UniTask.ToCoroutine(async () =>
        {
            var repository = new Repository();
            var storage = new LocalMemoryStorage();
            var driver = new VersionControlStorageDriver(repository, storage);
            repository.AddStorage(driver);
            
            await using var store = await repository.CreateStore("test", GetTestData, CancellationToken.None);
            Assert.IsNotNull(store);
            Assert.AreEqual(33, store.value.id);
            Assert.AreEqual("default", store.value.name);
            
            var store2 = await repository.GetStore<TestData>("test", CancellationToken.None);
            Assert.IsNotNull(store2);
            Assert.AreEqual(33, store2.value.id);
            Assert.AreEqual("default", store2.value.name);
        });
        
        [UnityTest]
        public IEnumerator TestCreateStoreWithExistingData() => UniTask.ToCoroutine(async () =>
        {
            var repository = new Repository();
            var storage = new LocalMemoryStorage();
            var driver = new VersionControlStorageDriver(repository, storage);
            repository.AddStorage(driver);
            
            driver.Set("test", new TestData {id = 56, name = "custom"}, 1, CancellationToken.None).Forget();
            
            await using var store = await repository.CreateStore("test", GetTestData, CancellationToken.None);
            Assert.IsNotNull(store);
            Assert.AreEqual(56, store.value.id);
            Assert.AreEqual("custom", store.value.name);
            Assert.AreEqual(1, store.version);
        });
        
        [UnityTest]
        public IEnumerator TestStoreUpdateStorages() => UniTask.ToCoroutine(async () =>
        {
            var repository = new Repository();
            var storage = new LocalMemoryStorage();
            var driver = new VersionControlStorageDriver(repository, storage);
            repository.AddStorage(driver);
            
            driver.Set("test", new TestData {id = 56, name = "custom"}, 1, CancellationToken.None).Forget();
            
            await using var store = await repository.CreateStore("test", GetTestData, CancellationToken.None);
            store.value = new TestData {id = 57, name = "custom2"};
            await store.Commit(CancellationToken.None);
            await store.Sync(CancellationToken.None);
            Assert.IsNotNull(store);
            Assert.AreEqual(57, store.value.id);
            Assert.AreEqual("custom2", store.value.name);
            Assert.AreEqual(2, store.version);
        });
        
        [UnityTest]
        public IEnumerator TestStoreCreateAndReleaseBranch() => UniTask.ToCoroutine(async () =>
        {
            var repository = new Repository();
            var storage = new LocalMemoryStorage();
            var driver = new VersionControlStorageDriver(repository, storage);
            repository.AddStorage(driver);
            
            driver.Set("test", new TestData {id = 56, name = "custom"}, 1, CancellationToken.None).Forget();
            
            await using var store = await repository.CreateStore("test", GetTestData, CancellationToken.None);
            var releaseBetaBranch = store.Checkout("beta", true);
            store.value = new TestData {id = 57, name = "custom2"};
            Assert.AreEqual(57, store.value.id);
            Assert.AreEqual("custom2", store.value.name);
            Assert.AreEqual(1, store.version);
            Assert.AreEqual(true, store.isDirty);
            
            releaseBetaBranch.Dispose();
            
            Assert.AreEqual(56, store.value.id);
            Assert.AreEqual("custom", store.value.name);
            Assert.AreEqual(1, store.version);
            Assert.AreEqual(false, store.isDirty);
        });
        
        [UnityTest]
        public IEnumerator TestStoreCreateAndMergeBranch() => UniTask.ToCoroutine(async () =>
        {
            var repository = new Repository();
            var storage = new LocalMemoryStorage();
            var driver = new VersionControlStorageDriver(repository, storage);
            repository.AddStorage(driver);
            
            driver.Set("test", new TestData {id = 56, name = "custom"}, 1, CancellationToken.None).Forget();
            
            await using var store = await repository.CreateStore("test", GetTestData, CancellationToken.None);
            var releaseBetaBranch = store.Checkout("beta", true);
            store.value = new TestData {id = 57, name = "custom2"};

            store.Merge((from, to) =>
            {
                to.name = from.name;
                return to;
            });
            releaseBetaBranch.Dispose();
            
            Assert.AreEqual(56, store.value.id);
            Assert.AreEqual("custom2", store.value.name);
            Assert.AreEqual(1, store.version);
            Assert.AreEqual(true, store.isDirty);
        });
    }
}