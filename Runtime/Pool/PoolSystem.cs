using System;

namespace Metamatic {
    using System.Collections.Generic;
    using UnityEngine;

    [Obsolete("Use Pool class instead")]
    public class PoolSystem : MonoBehaviour
    {
        [System.Serializable]
        public class ScriptablePoolDictionary: SerializedDictionary<ScriptablePoolBase, int> {}
        public ScriptablePoolDictionary alwaysReservedPools;

        private void Start() {
            foreach(var poolReserve in alwaysReservedPools) {
                var pool = poolReserve.Key;
                pool.Prepare(poolReserve.Value);
            }
        }
        
        private class PoolInstancesDictionary: Dictionary<int, List<Component>> {}

        private static PoolInstancesDictionary instances = new PoolInstancesDictionary();
        
        /// <summary>
        /// Get object from pool, or instantiate prefab and get from pool
        /// </summary>
        /// <param name="component"></param>
        /// <returns></returns>
        public static T Boil<T>(T component)
        where T: Component
        {
            var id = component.gameObject.GetInstanceID();
            List<Component> pool;
            if (!instances.ContainsKey(id))
            {
                pool = instances[id] = new List<Component>();
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
                return (T)item;
            }
            
            var newBehaviour = InstanceOrigin.Clone(component);
            newBehaviour.gameObject.AddComponent<ObjectFromPool>();
            pool.Add(newBehaviour);
            return newBehaviour;
        }
    }
}