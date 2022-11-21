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
}
