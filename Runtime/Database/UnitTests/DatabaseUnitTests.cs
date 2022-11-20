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
        public bool SubscribeToDatabaseStoreFromMasterBranch()
        {
            var store = Database.Branch("master").GetStore<TestState>("Player", new TestState { a = 1, b = "x", c = 3.33f });
            var derivedState = store.GetState();
            return store.state.a == 1;
        }
    }
}