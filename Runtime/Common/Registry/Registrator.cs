using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Metamatic;
using Sirenix.OdinInspector;

namespace Metamatic.Experimental {
    public abstract class Registrator<T>: MonoBehaviour
        where T: UnityEngine.Object
    {
        [Tooltip("Register game object only when it is active and present in hierarchy")]
        public ScriptableRegistry[] active;
        [Tooltip("Register game object until it exists in game hierarchy")]
        public ScriptableRegistry[] permanent;

        void Awake() {
            T self = typeof(T) == typeof(GameObject)
                ? this.gameObject as T
                : GetComponent<T>();
            /* Register sub objects */
            foreach(var registry in permanent) {
                (registry as ScriptableRegistry<T>).Add(self);
            }
        }

        void OnEnable() {
            T self = typeof(T) == typeof(GameObject)
                ? this.gameObject as T
                : GetComponent<T>();
            /* Register sub objects */
            foreach(var registry in active) {
                (registry as ScriptableRegistry<T>).Add(self);
            }
        }

        /// <summary>
        /// This function is called when the behaviour becomes disabled or inactive.
        /// </summary>
        void OnDisable()
        {
            T self = typeof(T) == typeof(GameObject)
                ? this.gameObject as T
                : GetComponent<T>();
            /* Register sub objects */
            foreach(var registry in active) {
                (registry as ScriptableRegistry<T>).Remove(self);
            }
        }

        void OnDestroy() {
            T self = typeof(T) == typeof(GameObject)
                ? this.gameObject as T
                : GetComponent<T>();
            /* Register sub objects */
            foreach(var registry in permanent) {
                (registry as ScriptableRegistry<T>).Remove(self);
            }
        }
    }
}