namespace Metamatic.GameAccessManagment {
    using UnityEngine;
    
    /// <summary>
    /// Verify several conditions with OR logic
    /// </summary>
    [CreateAssetMenu(fileName = "OwnCondition", menuName = "Metamatic/Conditions/OwnCondition", order = 0)]
    public class OwnCondition: ScriptableCondition, ICondition {
        public override bool Verify(Object target, Passes passes)
        {
            var assetGUID = AddressablesManager.GetAssetGUIDByInstance(target);
            if (assetGUID == null) {
                return false;
            }

            return passes.ContainsKey(assetGUID);
        }
    }
}