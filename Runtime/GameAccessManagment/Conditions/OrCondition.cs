namespace Metamatic.GameAccessManagment {
    using UnityEngine;

    [CreateAssetMenu(fileName = "OrCondition", menuName = "Metamatic/Conditions/OrCondition", order = 0)]
    /// <summary>
    /// Verify several conditions with OR logic
    /// </summary>
    public class OrCondition: ScriptableCondition, ICondition {
        [SerializeReference]
        public ScriptableCondition[] scriptableConditions;
        public override bool Verify(Pass[] passes) {
            if (scriptableConditions == null) {
                return false;
            }
            foreach(var condition in scriptableConditions) {
                if (condition != null) {
                    if (condition.Verify(passes)) {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}