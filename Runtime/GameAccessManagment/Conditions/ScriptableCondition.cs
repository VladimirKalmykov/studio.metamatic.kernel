namespace Metamatic.GameAccessManagment {
    using UnityEngine;
    public class ScriptableCondition: ScriptableObject, ICondition {
        public string message = "No access";
        public virtual bool Verify(UnityEngine.Object target, Passes passes) {
            return false;
        }
    }
}