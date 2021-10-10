namespace Metamatic {
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public class PoolSystem : MonoBehaviour
    {
        [System.Serializable]
        public class ScriptablePoolDictionary: SerializedDictionary<ScriptablePoolBase, int> {}
        public ScriptablePoolDictionary alwaysReservedPools;

        void Start() {
            foreach(var poolReserve in alwaysReservedPools) {
                var pool = poolReserve.Key;
                pool.Prepare(poolReserve.Value);
            }
        }
    }
}