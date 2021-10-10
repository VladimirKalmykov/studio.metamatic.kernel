namespace Metamatic.GameAccessManagment {
    using UnityEngine;

    [CreateAssetMenu(fileName = "CodeCondition", menuName = "Metamatic/Conditions/CodeCondition", order = 0)]
    /// <summary>
    /// Character must have pass MinLevelPass and its level must be greater or equal to minLevel
    /// </summary>
    public class CodeCondition: ScriptableCondition, ICondition {
        public string code = "";
        public override bool Verify(Pass[] passes) {
            if (passes == null) {
                return false;
            }
            foreach(CodePass pass in passes) {
                if (pass != null) {
                    if (pass.code == code) {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}