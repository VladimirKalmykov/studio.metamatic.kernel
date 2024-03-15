
using System;

namespace Metamatic.GameAccessManagment {
    using UnityEngine;
    using Sirenix.OdinInspector;

    [System.Serializable]
    public class GameAccessConfig: ISerializationCallbackReceiver
    {
        [Tooltip("Asset Id")] public string vendorCode;
        [Tooltip("Can view, but cannot iterate")]
        public ScriptableCondition view;
        [Tooltip("Can view and interact, but cannot use for free")]
        public ScriptableCondition interact;
        [Tooltip("Can view, interact and use for free")]
        public bool certify = false;
        [ShowIf("certify"), Tooltip("Full access"), GUIColor(0.89f, 0.78f, 0.1f, 1f)]
        public Certificate[] certificates;

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