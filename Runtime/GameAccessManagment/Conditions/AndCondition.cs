namespace Metamatic.GameAccessManagment {
    using UnityEngine;

    /// <summary>
    /// Verify several conditions with AND logic
    /// </summary>
    [CreateAssetMenu(fileName = "AndCondition", menuName = "Metamatic/Conditions/AndCondition", order = 0)]
    public class AndCondition: ScriptableCondition, ICondition {
        [SerializeReference]
        public ScriptableCondition[] scriptableConditions;
        public override bool Verify(Object target, Passes passes) {
            if (scriptableConditions == null) {
                return false;
            }
            foreach(var condition in scriptableConditions) {
                if (condition != null) {
                    if (!condition.Verify(target, passes)) {
                        return false;
                    }
                }
            }
            return false;
        }
    }
}