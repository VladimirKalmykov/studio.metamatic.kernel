using System;
using System.Collections;
using System.Threading.Tasks;
using Metamatic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Task = System.Threading.Tasks.Task;

public class DatabaseCommonTests
{
    public struct TestState
    {
        public int a;
    }

    private const string ENTITY_NAME = "UnitTestEntity";

    // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
    // `yield return null;` to skip a frame.
    [UnityTest]
    public IEnumerator GetStoreOfUnexistenBranchCreateBranchRemoveBranch()
    {
        // Make sure branch is removed before starting
        Database.RemoveBranch("testbranch");
        var store = Database.Branch("testbranch").GetStore(ENTITY_NAME, new TestState { a = 1 });
        /* Branch doesn't not exists */
        Assert.False(Database.BranchExists("testbranch"), "Branch should not be exist");
        store.SetState((ref TestState state) =>
        {
            state.a = 2;
        });
        Assert.True(Database.BranchExists("testbranch"), "Branch should be exists");
        Assert.True(store.GetState().a == 2, "State should be 2");
        Database.RemoveBranch("testbranch");
        Assert.False(Database.BranchExists("testbranch"), "Branch should not be exists after removing");
        Assert.True(store.GetState().a == 1, "State should be 1");
        yield return null;
    }
    
    [UnityTest]
    public IEnumerator SubscribeStateChange()
    {
        Database.RemoveBranch("testbranch");
        var store = Database.Branch("testbranch").GetStore(ENTITY_NAME, new TestState { a = 1 });
        var subscribtionHandleCount = 0;
        var asyncOp = new Promise<bool>(async (resolve, reject, self) =>
        {
            store.Subscribe((ref TestState state, ref TestState prevState) =>
            {
                subscribtionHandleCount++;
                if (subscribtionHandleCount == 1)
                {
                    Assert.True(state.a == 1 && prevState.a == 1, "First subscription handle call should accept state.a == 1");
                }
                else if (subscribtionHandleCount == 2)
                {
                    Assert.True(state.a == 2 && prevState.a == 1, $"Second subscription handle call should accept state.a == 2 but got {state.a}");
                    resolve(true);
                }
            });
            await AsyncUtils.ForgetCancellation(Task.Delay(10000, self.executionCancellationToken));
            resolve(false);
            // reject(new Exception("Timeout"));
        }).task;
        store.SetState((ref TestState state) =>
        {
            state.a = 2;
        });
        yield return AsyncUtils.EnumeratorAwaitable(asyncOp);
        Assert.True(subscribtionHandleCount == 2, "Subscribtion handle count should be 2");
        yield return null;
    }

    [UnityTest]
    public IEnumerator SubscribeProxyBranch()
    {
        Database.Clear();
        var storeA = Database.Branch("master").GetStore(ENTITY_NAME, new TestState { a = 7 });
        var storeB = Database.Branch("Round1").GetStore(ENTITY_NAME, new TestState { a = 8 });
        Assert.True(storeA.GetState().a == 7, "Store A property a should be 7");
        Assert.True(storeB.GetState().a == 8, "Store B property a should be 8");
        storeA.SetState((ref TestState state) =>
        {
            state.a = 2;
        });
        Assert.True(storeA.GetState().a == 2, "Store A property a should be 2");
        Assert.True(storeB.GetState().a == 2, "Store B property a should be 2");
        Assert.True(storeB.isOriginal == false, "Store B should not be original");
        storeB.SetState((ref TestState state) =>
        {
            state.a = 3;
        });
        Assert.True(storeB.isOriginal == true, "Store B should be original");
        Assert.True(storeA.GetState().a == 2, "Store A property a should be 2");
        Assert.True(storeB.GetState().a == 3, $"Store B property a should be 3, but got {storeB.GetState().a}");
        Database.Clear();
        yield return null;
    }
    
    [UnityTest]
    public IEnumerator ClearDatabase()
    {
        Action unsubscribe = null;
        try
        {
            Database.Clear();
            var storeA = Database.Branch("master").GetStore(ENTITY_NAME, new TestState { a = 7 });
            var storeChangedCount = 0;
            unsubscribe = storeA.debugObservableStore.Subscribe(store =>
            {
                storeChangedCount++;
            });
            storeA.SetState((ref TestState state) =>
            {
                state.a = 17;
            });
            Assert.AreEqual(1, storeChangedCount, "Store should be changed 1 times after set state");
            Assert.AreEqual(17, storeA.GetState().a, "Should be 17 after set state");
            Database.Save();
            Database.Clear();
            Assert.AreEqual(2, storeChangedCount, "Store should be changed 2 times after set state");
            Assert.AreEqual(7, storeA.GetState().a, "Should be 7 after clean db");
        }
        finally
        {
            unsubscribe?.Invoke();
        }
        yield return null;
    }

    [UnityTest]
    public IEnumerator SaveAndLoad()
    {
        Database.Clear();
        var storeA = Database.Branch("master").GetStore(ENTITY_NAME, new TestState { a = 7 });
        storeA.SetState((ref TestState state) =>
        {
            state.a = 17;
        });
        Assert.AreEqual(17, storeA.GetState().a, "Should be 17 after set state");
        Database.Save();
        Database.Clear();
        Assert.AreEqual(7, storeA.GetState().a, "Should be 7 after clean db");
        storeA.Load();
        Assert.AreEqual(17, storeA.GetState().a, "Should be 17 after load data");
        yield return null;
    }
    
    
}
