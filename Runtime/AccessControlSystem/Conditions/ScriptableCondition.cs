namespace DarkMatter.GameAccessManagment {
    using UnityEngine;
    public class ScriptableCondition: ScriptableObject, ICondition {
        public string message = "No access";
        public virtual bool Verify(Pass[] passes) {
            return false;
        }
    }
}