using Sirenix.OdinInspector;
using UnityEngine;

namespace Metamatic.UnitTests
{
    public struct TestState
    {
        public int a;
        public string b;
        public float c;
    }
    public class DatabaseUnitTests : MonoBehaviour
    {
        [Button]
        public static void SubscribeToDatabaseStoreFromMasterBranch()
        {
            var store = Database.Branch("master").GetStore("Player", new TestState { a = 1, b = "x", c = 3.33f });
            Debug.Log($"UNIT: SubscribeToDatabaseStoreFromMasterBranch #1: {store.GetState().a} expect 1");;
            store.Subscribe((ref TestState state, ref TestState prevState) =>
            {
                Debug.Log($"UNIT: SubscribeToDatabaseStoreFromMasterBranch #2: {state.a} vs {prevState.a}");
            });
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