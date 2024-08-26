using System;

namespace Metamatic {
    using UnityEngine;

    [Obsolete("Use Dispatcher events instead", true)]
    [PluginFor(typeof(BehaviourState))]
    public class GameObjectNameToBehaviourState: MonoBehaviour {
        BehaviourState behaviourState;
        public string keyName = "name";
        public bool realtime;
        BehaviourState.StateValue<string> stateValue;

        void Start() {
            behaviourState = GetComponentInParent<BehaviourState>();
            stateValue = behaviourState.Init<string>(keyName);
            stateValue.SetValue(name);
        }

        string m_CachedName = "";
        void Update() {
            if (realtime && m_CachedName != name) {
                m_CachedName = name;
                stateValue.SetValue(name);
            }
        }
    }
}