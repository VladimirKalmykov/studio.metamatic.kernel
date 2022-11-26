using System;
using System.Collections;
using System.Threading.Tasks;
using Metamatic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Task = System.Threading.Tasks.Task;

public class DatabaseCommonTests
{
    public struct TestState
    {
        public int a;
    }

    // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
    // `yield return null;` to skip a frame.
    [UnityTest]
    public IEnumerator GetStoreOfUnexistenBranchCreateBranchRemoveBranch()
    {
        // Make sure branch is removed before starting
        Database.RemoveBranch("testbranch");
        var store = Database.Branch("testbranch").GetStore("Player", new TestState { a = 1 });
        /* Branch doesn't not exists */
        Assert.False(Database.BranchExists("testbranch"));
        store.SetState((ref TestState state) =>
        {
            state.a = 2;
        });
        Assert.True(Database.BranchExists("testbranch"));
        Assert.True(store.GetState().a == 2);
        Database.RemoveBranch("testbranch");
        Assert.False(Database.BranchExists("testbranch"));
        Assert.True(store.GetState().a == 1);
        yield return null;
    }
    
    [UnityTest]
    public IEnumerator SubscribeStateChange()
    {
        Database.RemoveBranch("testbranch");
        var store = Database.Branch("testbranch").GetStore("Player", new TestState { a = 1 });
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
        var storeA = Database.Branch("master").GetStore("Player", new TestState { a = 7 });
        var storeB = Database.Branch("Round1").GetStore("Player", new TestState { a = 8 });
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
}
