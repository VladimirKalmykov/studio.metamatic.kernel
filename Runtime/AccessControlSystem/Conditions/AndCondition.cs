namespace DarkMatter.GameAccessManagment {
    using UnityEngine;

    [CreateAssetMenu(fileName = "AndCondition", menuName = "Dark Matter/Conditions/AndCondition", order = 0)]
    /// <summary>
    /// Verify several conditions with AND logic
    /// </summary>
    public class AndCondition: ScriptableCondition, ICondition {
        [SerializeReference]
        public ScriptableCondition[] scriptableConditions;
        public override bool Verify(Pass[] passes) {
            if (scriptableConditions == null) {
                return false;
            }
            foreach(var condition in scriptableConditions) {
                if (condition != null) {
                    if (!condition.Verify(passes)) {
                        return false;
                    }
                }
            }
            return false;
        }
    }
}