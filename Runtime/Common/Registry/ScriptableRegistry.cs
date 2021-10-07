namespace Metamatic {
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using Sirenix.OdinInspector;

    public abstract class ScriptableRegistry: ScriptableObject {
        public virtual Type GetRegistryType() { return default(Type); }
    }

    public abstract class ScriptableRegistry<T>: ScriptableRegistry, IRegistry<T> {
        [Title("Description", bold: false)]
        [HideLabel]
        [MultiLineProperty(2)]
        public string description = "";
        [Tooltip("Can contains only one record")]
        public bool single = false;
        [ShowInInspector, ReadOnly]
        public List<T> items {
            get; protected set;
        } = new List<T>();
        public event Action<T> OnAdd;
        public event Action<T> OnRemove;
        public event Action<T[]> OnChange;
        public event Action<T> OnFirstChange;
        public T first {
            get {
                return items.Count > 0
                    ? items[0]
                    : default(T);
            }
        }
        public void Add(T item) {
            if (single && items.Count > 0) {
                var current = items[0];
                items.Clear();
                OnRemove?.Invoke(current);
            }
            items.Add(item);
            OnAdd?.Invoke(item);
            if (items.Count == 1) {
                OnFirstChange?.Invoke(item);
            }
            OnChange?.Invoke(items.ToArray());
        }
        public void Remove(T item) {
            if (items.Contains(item)) {
                items.Remove(item);

                if (items.Count == 0) {
                    OnFirstChange?.Invoke(default(T));
                }
                OnRemove?.Invoke(item);
            }
        }
        public Action Subscribe(Action<T> addHandler, Action<T> removeHandler) {
            foreach(var record in items) {
                addHandler(record);
            }
            OnAdd += addHandler;
            OnRemove += removeHandler;

            return () => {
                Unsubscribe(addHandler, removeHandler);
            };
        }

        public void Unsubscribe(Action<T> addHandler, Action<T> removeHandler) {
            OnAdd -= addHandler;
            OnRemove -= removeHandler;
        }

        public Action SubscribeFirst(Action<T> changeHandler) {
            changeHandler(first);
            OnFirstChange += changeHandler;

            return () => {
                UnsubscribeFirst(changeHandler);
            };
        }

        public void UnsubscribeFirst(Action<T> changeHandler) {
            OnFirstChange -= changeHandler;
        }

        public override Type GetRegistryType()
        {
            return typeof(T);
        } 
    }
}