namespace Metamatic.GameAccessManagment {
    using UnityEngine;
    /// <summary>
    /// Character must have at least one pass of any type
    /// </summary>
    [CreateAssetMenu(fileName = "HasPassesCondition", menuName = "Metamatic/Conditions/HasPassesCondition", order = 0)]
    public class HasPassesCondition: ScriptableCondition, ICondition
    {
        public override bool Verify(Pass[] passes) {
            return passes != null && passes.Length > 0;
        }
    }
}