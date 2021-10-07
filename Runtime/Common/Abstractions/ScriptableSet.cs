/* Deprecation candidate! */
namespace Metamatic {
    using System;
    using UnityEngine;
    using System.Collections.Generic;
    using Sirenix.OdinInspector;

    public abstract class ScriptableSet<T>: ScriptableObject, ISet<T> {
        public Type type {
            get; private set;
        } = typeof(T);

        [AssetSelector]
        [ListDrawerSettings(DraggableItems = false, Expanded = true, ShowPaging = false, ShowItemCount = false)]
        public List<T> items = new List<T>();

        public int Count {
            get {
                return items.Count;
            }
        }

         public T GetRandom() {
            return items.Count == 0
                ? default(T)
                : items[UnityEngine.Random.Range(0, items.Count - 1)];
        }
    }
}