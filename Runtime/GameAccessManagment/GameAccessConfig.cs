
using System;

namespace Metamatic.GameAccessManagment {
    using UnityEngine;
    using Sirenix.OdinInspector;
    
    [Serializable]
    public class ToggleableScriptableCondition: Toggleable<ScriptableCondition>
    {
        
    }

    [System.Serializable]
    public class GameAccessConfig: ISerializationCallbackReceiver
    {
        [Tooltip("Asset Id")] public string vendorCode;
        [Tooltip("Can view, but cannot iterate")]
        public ToggleableScriptableCondition view;
        [Tooltip("Can view and interact, but cannot use")]
        public ToggleableScriptableCondition interact;
        [Tooltip("Can view, interact and use")]
        public ToggleableScriptableCondition use;

        public void OnBeforeSerialize()
        {
            if (string.IsNullOrEmpty(vendorCode))
            {
                vendorCode = Guid.NewGuid().ToString();
            }
        }

        public void OnAfterDeserialize()
        {
            // Empty
        }
    }
}