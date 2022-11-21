using Sirenix.OdinInspector;
using UnityEngine;

namespace Metamatic.UnitTests
{
    public struct TestState
    {
        public int a;
    }
    public class DatabaseUnitTests : MonoBehaviour
    {
        [Button]
        public static void SubscribeToDatabaseStoreFromMasterBranch()
        {
            var store = Database.Branch("master").GetStore("Player", new TestState { a = 1 });
            store.Subscribe((ref TestState state, ref TestState prevState) =>
            {
                Debug.Log($"UNIT: SubscribeToDatabaseStoreFromMasterBranch #2: {state.a} vs {prevState.a}");
            });
        }

        [Button]
        public static void ChangeState()
        {
            var store = Database.Branch("master").GetStore<TestState>("Player");
            store.SetState((ref TestState state) =>
            {
                state.a = 2;
            });
            store.SetState((ref TestState state) =>
            {
                state.a = 3;
            });
        }
    }
}