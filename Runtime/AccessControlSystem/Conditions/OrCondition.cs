namespace DarkMatter.GameAccessManagment {
    using UnityEngine;

    [CreateAssetMenu(fileName = "OrCondition", menuName = "Dark Matter/Conditions/OrCondition", order = 0)]
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