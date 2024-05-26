using System;
using System.Threading.Tasks;

namespace Metamatic
{
    using System.Collections;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.TestTools;
    

    public class DatabasePlaymodeTests : MonoBehaviour
    {
        [Serializable]
        public struct TestState
        {
            public int a;
        }
        
        [Serializable]
        public struct TestState2
        {
            public int a;
        }
        public const string ENTITY_NAME = "DatabasePlaymodeTestEntity";
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
            DatabaseVirtualStore<TestState> store = null;
            try
            {
                Database.Clear();
                Database.RemoveBranch("testbranch");
                store = Database.Branch("testbranch").GetStore("Player", new TestState {a = 1});
                var subscribtionHandleCount = 0;
                var asyncOp = new Promise<bool>(async (resolve, reject, self) =>
                {
                    store.Subscribe((ref TestState state, ref TestState prevState) =>
                    {
                        try
                        {
                            subscribtionHandleCount++;
                            if (subscribtionHandleCount == 1)
                            {
                                Assert.True(state.a == 1 && prevState.a == 1,
                                    $"First subscription handle call should accept state.a == 1, but got {state.a}, {prevState.a}");
                            }
                            else if (subscribtionHandleCount == 2)
                            {
                                Assert.True(state.a == 2 && prevState.a == 1,
                                    $"Second subscription handle call should accept state.a == 2 but got {state.a}");
                                resolve(true);
                            }
                        }
                        catch
                        {
                            resolve(false);
                        }
                    });
                    await AsyncUtils.ForgetCancellation(Task.Delay(10000, self.executionCancellationToken));
                    resolve(false);
                    // reject(new Exception("Timeout"));
                }).task;
                store.SetState((ref TestState state) => { state.a = 2; });
                yield return AsyncUtils.EnumeratorAwaitable(asyncOp);
                Assert.True(asyncOp.Result == true, "Promise must return true");

                Assert.True(subscribtionHandleCount == 2, "Subscribtion handle count should be 2");
            }
            finally
            {
                store?.Dispose();
            }
            yield return null;
        }
        
        [UnityTest]
        public IEnumerator SaveAll()
        {
            DatabaseVirtualStore<TestState> storeA = null;
            DatabaseVirtualStore<TestState> storeB = null;
            try
            {
                Database.Clear();
                storeA = Database.Branch("master").GetStore("Player", new TestState {a = 7});
                storeB = Database.Branch("Round1").GetStore("Player", new TestState {a = 8});
                storeA.SetState((ref TestState state) => { state.a = 2; });
                Assert.True(storeA.GetState().a == 2, "Store A property a should be 2");
                Assert.True(Database.Save(), "Save with no errors");
                Database.Clear();
                var afterSaveStore = Database.Branch("master").GetStore("Player", new TestState {a = 7});
                Assert.True(afterSaveStore.GetState().a == 7, "Store A property a should be 7 before load");
                afterSaveStore.Load();
                Assert.True(afterSaveStore.GetState().a == 2, "Store A property a should be 2 after load");
            }
            finally
            {
                storeA?.Dispose();
                storeB?.Dispose();
            }
            
            yield return null;
        }
        
        [UnityTest]
        public IEnumerator SaveOneEntityManyStores()
        {
            DatabaseVirtualStore<TestState> storeA = null;
            DatabaseVirtualStore<TestState2> storeB = null;
            try
            {
                Database.Clear();
                storeA = Database.Branch("master").GetStore("Player", new TestState { a = 7 });
                storeB = Database.Branch("master").GetStore("Player", new TestState2 { a = 8 });
                storeA.SetState((ref TestState state) =>
                {
                    state.a = 2;
                });
                storeB.SetState((ref TestState2 state) =>
                {
                    state.a = 3;
                });
                Assert.True(storeA.GetState().a == 2, "Store A property a should be 2");
                Assert.True(storeB.GetState().a == 3, "Store B property a should be 3");
                Assert.True(Database.Save(), "Save with no errors");
                Database.Clear();
                var afterSaveStoreA = Database.Branch("master").GetStore("Player", new TestState {a = 7});
                var afterSaveStoreB = Database.Branch("master").GetStore("Player", new TestState2 {a = 8});
                afterSaveStoreA.Load();
                Assert.True(afterSaveStoreA.GetState().a == 2, "Store A property a should be 2 after load");
                afterSaveStoreB.Load();
                Assert.True(afterSaveStoreA.GetState().a == 2, "Store A property a should be 2 after load");
                Assert.True(afterSaveStoreB.GetState().a == 3, "Store B property a should be 3 after load");
            }
            finally
            {
                storeA?.Dispose();
                storeB?.Dispose();
            }
            yield return null;
        }
        
        // [Serializable]
        // public struct UnitTestMyScriptableObject {
        //     public DatabaseScriptableObjectForUnitTests myScriptableObject;
        // }
        // [UnityTest]
        // public IEnumerator SaveAndLoadDataWithScriptableObjectAsRef()
        // {
        //     string assetPathname = "";
        //     string folderPathname = "";
        //     DatabaseVirtualStore<UnitTestMyScriptableObject> storeA = null;
        //     try
        //     {
        //         Database.Clear();
        //         storeA = Database.Branch("master").GetStore(ENTITY_NAME, new UnitTestMyScriptableObject { });
        //         Assert.IsNull(storeA.GetState().myScriptableObject, "Should be null at initial moment");
        //     
        //         /* Create asset */
        //         string guid = AssetDatabase.CreateFolder("Assets", "UnitTest_Temportaty_Files");
        //         folderPathname = AssetDatabase.GUIDToAssetPath(guid);
        //         Assert.True(AssetDatabase.IsValidFolder(folderPathname), "Temportaty folder should be valid");
        //         Debug.Log($"Temporary folder just created at {folderPathname}");
        //
        //         // Create testing scriptable object
        //         DatabaseScriptableObjectForUnitTests asset = ScriptableObject.CreateInstance<DatabaseScriptableObjectForUnitTests>();
        //         assetPathname = $"{folderPathname}/SaveAndLoadDataWithScriptableObjectDoodle.asset";
        //         AssetDatabase.CreateAsset(asset, assetPathname);
        //         AssetDatabase.SaveAssets();
        //     
        //         storeA.SetState((ref UnitTestMyScriptableObject state) =>
        //         {
        //             state.myScriptableObject = asset;
        //         });
        //         Assert.AreEqual(asset, storeA.GetState().myScriptableObject, "State should has created scriptable object");
        //         Database.Save();
        //         Database.Clear();
        //         Assert.IsNull(storeA.GetState().myScriptableObject, "Should be null after clear");
        //         // storeA.Load();
        //         // Assert.AreEqual(17, storeA.GetState().a, "Should be 17 after load data");
        //     }
        //     finally
        //     {
        //         storeA?.Dispose();
        //         if (assetPathname.Length > 0)
        //         {
        //             AssetDatabase.DeleteAsset(assetPathname);
        //         }
        //
        //         if (folderPathname.Length > 0)
        //         {
        //             FileUtil.DeleteFileOrDirectory(folderPathname);
        //         }
        //     }
        //     yield return null;
        // }
    }

}