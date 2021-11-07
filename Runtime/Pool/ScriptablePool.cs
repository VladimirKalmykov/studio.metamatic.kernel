using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;

namespace Metamatic {
    public abstract class ScriptablePool<T>: ScriptablePoolBase, IFactory<T>
        where T: Component
    {
        [AssetsOnly]
        public T prefab;
        public int limitMin = 1;
        public int limitMax = 99999;
        public float cullingTime = 10f;
        // TODO: culling

        protected List<T> prewarmed = new List<T>();
        /// <summary>
        /// Total prewarmed count
        /// </summary>
        protected int reservedCount = 0;

        public T[] Get(int count = 1, Transform parent = null, System.Action<T> beforeEnableHook = null) {
            var index = 0;
            var result = new T[count];
            for (var i = prewarmed.Count - 1; i >= 0; i--) {
                var component = prewarmed[i];
                if (component == null) {
                    /* Remove deleted component */
                    prewarmed.RemoveAt(i);
                    continue;
                }
                /* Check component already in use */
                if (component.gameObject.activeInHierarchy) continue;
                result[index] = component;
                component.transform.SetParent(parent);
                beforeEnableHook?.Invoke(component);
                /* Activate right now to prevent situation when two scripts got the same game object */
                component.gameObject.SetActive(true);
                index++;
                if (index >= count) {
                    break;
                }
            }
            #if UNITY_EDITOR
            if (index != count) {
                Debug.LogWarning($"Pool `${name}` is overfilled with ${count - index} items");
            }
            #endif
            /* Filled missing components */
            for (var i = index; i <= count - 1; i++) {
                #if UNITY_EDITOR
                if (prefab == null) {
                    throw new System.Exception("Uncompleted scriptable pool");
                }
                #endif
                var newGo = Instantiate(prefab.gameObject, parent, true);
                var gameObjectFromPool = newGo.GetComponent<GameObjectFromPool>() ?? newGo.AddComponent<GameObjectFromPool>();
                var component = newGo.GetComponent<T>();
                beforeEnableHook?.Invoke(component);
                prewarmed.Add(component);
                result[i] = component;
                newGo.SetActive(true);
            }
            return result;
        }

        public T Get() {
            var items = Get(1);
            if (items.Length == 0) {
                return null;
            }
            return items[0];
        }

        public T Pull() {
            return Get();
        }

        public T Pull(bool fromPool) {
            return Get();
        }

        public T[] PullMany(int count) {
            return Get(count);
        }

        public T[] PullMany(int count, bool fromPool = true) {
            return Get(count);
        }

        protected override void OnPrewarm(int count) {
            reservedCount += count;
            for (var i = 0; i < count; i++) {
                var newGo = Instantiate(prefab.gameObject);
                newGo.SetActive(false);
                newGo.transform.SetParent(null);
                var component = newGo.GetComponent<T>();
                prewarmed.Add(component);
            }
        }

        protected override void OnPrewarmRelease(int count) {
            reservedCount -= count;

            for (var i = prewarmed.Count - 1; i >= 0; i--) {
                var component = prewarmed[i];
                if (prewarmed.Count > reservedCount && (component == null || !component.gameObject.activeInHierarchy)) {
                    prewarmed.RemoveAt(i);
                    continue;
                }
            }
        }
    }
}