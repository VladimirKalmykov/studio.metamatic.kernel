namespace Metamatic {
    using UnityEngine;

    [PluginFor(typeof(BehaviourState))]
    public class GameObjectNameToBehaviourState: MonoBehaviour {
        BehaviourState behaviourState;
        public string keyName = "name";
        public bool realtime = false;
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