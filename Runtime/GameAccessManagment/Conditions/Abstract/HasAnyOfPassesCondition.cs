namespace Metamatic.GameAccessManagment {
    using UnityEngine;
    
    public abstract class HasAnyOfPassesCondition: ScriptableCondition, ICondition
    {
        public abstract Pass[] passes { get; }
        public override bool Verify(Pass[] passes) {
            return passes != null && passes.Length > 0;
        }
    }
}