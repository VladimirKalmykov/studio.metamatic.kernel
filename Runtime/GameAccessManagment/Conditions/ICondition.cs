namespace Metamatic.GameAccessManagment {
    public interface ICondition {
        public bool Verify(UnityEngine.Object target, Passes passes);
    }
}