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
            var self = typeof(T) == typeof(GameObject)
                ? this.gameObject as T
                : GetComponent<T>();
            /* Register sub objects */
            foreach(var registry in permanent) {
                (registry as ScriptableRegistry<T>).Add(self);
            }
        }

        void OnEnable() {
            var self = typeof(T) == typeof(GameObject)
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
            var self = typeof(T) == typeof(GameObject)
                ? this.gameObject as T
                : GetComponent<T>();
            /* Register sub objects */
            foreach(var registry in active) {
                (registry as ScriptableRegistry<T>).Remove(self);
            }
        }

        void OnDestroy() {
            var self = typeof(T) == typeof(GameObject)
                ? this.gameObject as T
                : GetComponent<T>();
            /* Register sub objects */
            foreach(var registry in permanent) {
                (registry as ScriptableRegistry<T>).Remove(self);
            }
        }
    }
}