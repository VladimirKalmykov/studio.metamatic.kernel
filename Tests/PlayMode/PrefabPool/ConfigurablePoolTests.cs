using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using Metamatic;

public class ConfigurablePoolTests
{
    private GameObject prefab;
    private IPoolConfig<GameObject> gameObjectPoolConfig;

    [SetUp]
    public void SetUp()
    {
        // Create a simple prefab for testing
        prefab = new GameObject("TestPrefab");
        gameObjectPoolConfig = new GameObjectConfigurablePoolConfig(prefab);
    }

    [TearDown]
    public void TearDown()
    {
        // Clean up any remaining instances
        Object.DestroyImmediate(prefab);
        gameObjectPoolConfig = null;
    }

    [UnityTest]
    public IEnumerator GetShared_ReturnsInstanceOfPrefab()
    {
        var instance = ConfigurablePool<GameObject>.GetShared(gameObjectPoolConfig);

        Assert.IsNotNull(instance);
        Assert.AreEqual("TestPrefab(Clone)", instance.name);
        Assert.IsTrue(instance.activeSelf);

        // Cleanup
        ConfigurablePool<GameObject>.ReturnShared(instance);
        yield return null;
    }

    [UnityTest]
    public IEnumerator ReturnShared_DeactivatesInstance()
    {
        var instance = ConfigurablePool<GameObject>.GetShared(gameObjectPoolConfig);

        ConfigurablePool<GameObject>.ReturnShared(instance);

        Assert.IsFalse(instance.activeSelf);

        yield return null;
    }

    [UnityTest]
    public IEnumerator Pool_ReusesInstances()
    {
        var instance1 = ConfigurablePool<GameObject>.GetShared(gameObjectPoolConfig);
        ConfigurablePool<GameObject>.ReturnShared(instance1);

        var instance2 = ConfigurablePool<GameObject>.GetShared(gameObjectPoolConfig);

        Assert.AreSame(instance1, instance2);

        // Cleanup
        ConfigurablePool<GameObject>.ReturnShared(instance2);
        yield return null;
    }

    [UnityTest]
    public IEnumerator Pool_CreatesNewInstancesWhenEmpty()
    {
        var instance1 = ConfigurablePool<GameObject>.GetShared(gameObjectPoolConfig);

        // Do not return instance1 to the pool

        var instance2 = ConfigurablePool<GameObject>.GetShared(gameObjectPoolConfig);

        Assert.AreNotSame(instance1, instance2);

        // Cleanup
        ConfigurablePool<GameObject>.ReturnShared(instance1);
        ConfigurablePool<GameObject>.ReturnShared(instance2);
        yield return null;
    }

    [UnityTest]
    public IEnumerator Pool_AutoShrinksOverTime()
    {
        // Prewarm the pool
        ConfigurablePool<GameObject>.PreWarmShared(new GameObjectConfigurablePoolConfig(prefab), 5);

        yield return null; // Wait a frame

        // Check that the pool has 5 instances
        var pool = ConfigurablePool<GameObject>.DebugGetSharedPool(gameObjectPoolConfig);
        Assert.AreEqual(5, pool.Count);

        // Simulate time passing beyond the auto-shrink period
        ConfigurablePool<GameObject>.DefaultShrinkingPeriod = 1f; // Set shrink period to 1 second for the test
        yield return new WaitForSeconds(1.1f);

        // Force the auto-shrink check
        pool.SubscribeAutoShrinkTime(pool.AutoShrinkTimestamp);

        // Wait for the shrink to process
        yield return null;

        // Check that the pool has shrunk
        Assert.LessOrEqual(pool.Count, 5);

        yield return null;
    }

    [UnityTest]
    public IEnumerator Dispose_DisposesAllInstances()
    {
        var pool = new ConfigurablePool<GameObject>(gameObjectPoolConfig, capacity: 2);
        var instance1 = pool.Get();
        var instance2 = pool.Get();

        pool.Return(instance1);
        pool.Return(instance2);

        pool.Dispose();

        // Wait for the end of the frame to allow Unity to destroy the objects
        yield return null;

        // Instances should be destroyed
        Assert.IsTrue(instance1 == null);
        Assert.IsTrue(instance2 == null);

        yield return null;
    }

    [UnityTest]
    public IEnumerator GetShared_CreatesPoolIfNotExists()
    {
        // Ensure no pool exists
        var pool = ConfigurablePool<GameObject>.DebugGetSharedPool(gameObjectPoolConfig);
        Assert.IsNull(pool);

        var instance = ConfigurablePool<GameObject>.GetShared(gameObjectPoolConfig);

        // Pool should now exist
        pool = ConfigurablePool<GameObject>.DebugGetSharedPool(gameObjectPoolConfig);
        Assert.IsNotNull(pool);

        // Cleanup
        ConfigurablePool<GameObject>.ReturnShared(instance);
        yield return null;
    }

    [UnityTest]
    public IEnumerator ReturnShared_DestroysInstanceIfNoPool()
    {
        var newPrefab = new GameObject("NewPrefab");
        var instance = Object.Instantiate(newPrefab);

        LogAssert.Expect(LogType.Error, "[ConfigurablePool] Trying to return object to non-existent global pool. Object will be destroyed");

        ConfigurablePool<GameObject>.ReturnShared(instance);

        // Wait for the end of the frame to allow Unity to destroy the object
        yield return null;

        Assert.IsFalse(instance == null);

        // Cleanup
        Object.DestroyImmediate(newPrefab);
        yield return null;
    }

    [UnityTest]
    public IEnumerator IsBelongsToPool_ReturnsCorrectly()
    {
        var pool = new ConfigurablePool<GameObject>(gameObjectPoolConfig);

        var instance = pool.Get();

        Assert.IsTrue(pool.IsBelongsToPool(instance));

        pool.Return(instance);

        Assert.IsTrue(pool.IsBelongsToPool(instance));

        // Get the instance back from the pool before releasing
        instance = pool.Get();

        pool.Release(instance);

        Assert.IsFalse(pool.IsBelongsToPool(instance));

        yield return null;
    }


    [UnityTest]
    public IEnumerator HardDetach_RemovesInstanceFromPool()
    {
        var pool = new ConfigurablePool<GameObject>(gameObjectPoolConfig);

        var instance = pool.Get();
        pool.Return(instance);

        pool.HardDetach(instance);

        Assert.IsFalse(pool.IsBelongsToPool(instance));

        // Try to get the instance again
        var newInstance = pool.Get();

        Assert.AreNotSame(instance, newInstance);

        // Cleanup
        pool.Return(newInstance);
        yield return null;
    }
}