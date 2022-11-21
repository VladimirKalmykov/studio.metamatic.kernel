using System.Threading.Tasks;

namespace Metamatic
{
    using System.Collections;
    using System.Collections.Generic;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.TestTools;
    
    public struct TestState
    {
        public int a;
    }

    public class DatabasePlaymodeTests : MonoBehaviour
    {

        [UnityTest]
        public IEnumerator DatabaseHasInstance()
        {
            Debug.Log("Check instance");
            Assert.True(Database.instance != null, "Database has instance");
            yield return true;
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

}