namespace DarkMatter.GameAccessManagment {
    using UnityEngine;

    [CreateAssetMenu(fileName = "MinLevelCondition", menuName = "Dark Matter/Conditions/MinLevelCondition", order = 0)]
    /// <summary>
    /// Character must have pass MinLevelPass and its level must be greater or equal to minLevel
    /// </summary>
    public class MinLevelCondition: ScriptableCondition, ICondition {
        public int minLevel = 1;
        public override bool Verify(Pass[] passes) {
            if (passes == null) {
                return false;
            }
            foreach(LevelPass pass in passes) {
                if (pass != null) {
                    if (pass.level >= minLevel) {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}