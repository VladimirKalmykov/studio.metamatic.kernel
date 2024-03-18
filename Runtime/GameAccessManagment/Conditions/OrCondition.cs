namespace Metamatic.GameAccessManagment {
    using UnityEngine;
    
    /// <summary>
    /// Verify several conditions with OR logic
    /// </summary>
    [CreateAssetMenu(fileName = "OrCondition", menuName = "Metamatic/Conditions/OrCondition", order = 0)]
    public class OrCondition: ScriptableCondition, ICondition {
        [SerializeReference]
        public ScriptableCondition[] scriptableConditions;
        public override bool Verify(Object target, Passes passes) {
            if (scriptableConditions == null) {
                return false;
            }
            foreach(var condition in scriptableConditions) {
                if (condition != null) {
                    if (condition.Verify(target, passes)) {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}