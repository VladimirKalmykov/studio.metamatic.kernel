namespace Metamatic {
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
        
        private class PoolInstancesDictionary: Dictionary<int, List<Behaviour>> {}

        private static PoolInstancesDictionary instances = new PoolInstancesDictionary();
        
        /// <summary>
        /// Get object from pool, or instantiate prefab and get from pool
        /// </summary>
        /// <param name="component"></param>
        /// <returns></returns>
        public static Behaviour Boil(Behaviour component)
        {
            var id = component.gameObject.GetInstanceID();
            List<Behaviour> pool;
            if (!instances.ContainsKey(id))
            {
                pool = instances[id] = new List<Behaviour>();
            }
            else
            {
                pool = instances[id];
            }

            var totalCount = pool.Count;
            var i = 0;
            while (i < totalCount)
            {
                var item = pool[i];
                if (item == null)
                {
                    pool.RemoveAt(i);
                    totalCount--;
                    i--;
                }

                i++;

                if (item.gameObject.activeInHierarchy) continue;
                item.gameObject.SetActive(true);
                return item;
            }
            
            var newBehaviour = (Behaviour) Instantiate(component.gameObject).GetComponent(component.GetType());
            pool.Add(newBehaviour);
            return newBehaviour;
        }
    }
}