using System;

namespace Metamatic {
    using UnityEngine;

    [Obsolete("Use Dispatcher events instead", true)]
    [CreateAssetMenu(fileName = "BehaviourStateRegistry", menuName = "Metamatic/Registry/BehaviourStateRegistry")]
    public class ScriptableBehaviourStateRegistry: ScriptableRegistry<BehaviourState> {
        
    }
}