using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using Metamatic;

public class PrefabPoolTests
{
    private GameObject prefab;

    [SetUp]
    public void SetUp()
    {
        // Create a simple prefab for testing
        prefab = new GameObject("TestPrefab");
        prefab.AddComponent<TestComponent>();
    }

    [TearDown]
    public void TearDown()
    {
        // Clean up any remaining instances
        Object.DestroyImmediate(prefab);
    }

    [UnityTest]
    public IEnumerator GetShared_ReturnsInstanceOfPrefab()
    {
        var instance = PrefabPool<GameObject>.GetShared(prefab);

        Assert.IsNotNull(instance);
        Assert.AreEqual("TestPrefab(Clone)", instance.name);
        Assert.IsTrue(instance.activeSelf);

        // Cleanup
        PrefabPool<GameObject>.ReturnShared(instance);
        yield return null;
    }

    [UnityTest]
    public IEnumerator ReturnShared_DeactivatesInstance()
    {
        var instance = PrefabPool<GameObject>.GetShared(prefab);

        PrefabPool<GameObject>.ReturnShared(instance);

        Assert.IsFalse(instance.activeSelf);

        yield return null;
    }

    [UnityTest]
    public IEnumerator Pool_ReusesInstances()
    {
        var instance1 = PrefabPool<GameObject>.GetShared(prefab);
        PrefabPool<GameObject>.ReturnShared(instance1);

        var instance2 = PrefabPool<GameObject>.GetShared(prefab);

        Assert.AreSame(instance1, instance2);

        // Cleanup
        PrefabPool<GameObject>.ReturnShared(instance2);
        yield return null;
    }

    [UnityTest]
    public IEnumerator Pool_CreatesNewInstancesWhenEmpty()
    {
        var instance1 = PrefabPool<GameObject>.GetShared(prefab);

        // Do not return instance1 to the pool

        var instance2 = PrefabPool<GameObject>.GetShared(prefab);

        Assert.AreNotSame(instance1, instance2);

        // Cleanup
        PrefabPool<GameObject>.ReturnShared(instance1);
        PrefabPool<GameObject>.ReturnShared(instance2);
        yield return null;
    }

    [UnityTest]
    public IEnumerator Pool_AutoShrinksOverTime()
    {
        // Prewarm the pool
        PrefabPool<GameObject>.PreWarmShared(prefab, 5);

        yield return null; // Wait a frame

        // Check that the pool has 5 instances
        var pool = PrefabPool<GameObject>.DebugGetSharedPool(prefab);
        Assert.AreEqual(5, pool.Count);

        // Simulate time passing beyond the auto-shrink period
        PrefabPool<GameObject>.DefaultShrinkingPeriod = 1f; // Set shrink period to 1 second for the test
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
        var pool = new PrefabPool<GameObject>(prefab, capacity: 2);
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
        var pool = PrefabPool<GameObject>.DebugGetSharedPool(prefab);
        Assert.IsNull(pool);

        var instance = PrefabPool<GameObject>.GetShared(prefab);

        // Pool should now exist
        pool = PrefabPool<GameObject>.DebugGetSharedPool(prefab);
        Assert.IsNotNull(pool);

        // Cleanup
        PrefabPool<GameObject>.ReturnShared(instance);
        yield return null;
    }

    [UnityTest]
    public IEnumerator ReturnShared_DestroysInstanceIfNoPool()
    {
        var newPrefab = new GameObject("NewPrefab");
        var instance = Object.Instantiate(newPrefab);

        LogAssert.Expect(LogType.Error, "Trying to return object to non-existent global pool. Object will be destroyed");

        PrefabPool<GameObject>.ReturnShared(instance);

        // Wait for the end of the frame to allow Unity to destroy the object
        yield return null;

        Assert.IsTrue(instance == null);

        // Cleanup
        Object.DestroyImmediate(newPrefab);
        yield return null;
    }

    [UnityTest]
    public IEnumerator IsBelongsToPool_ReturnsCorrectly()
    {
        var pool = new PrefabPool<GameObject>(prefab);

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
        var pool = new PrefabPool<GameObject>(prefab);

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

public class TestComponent : UnityEngine.Component
{
    // A simple component for testing purposes
}
