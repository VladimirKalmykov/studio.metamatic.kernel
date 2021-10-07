namespace Metamatic {
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Allows you to keep some key/values
    /// </summary>
    public class BehaviourState: MonoBehaviour {
        public ScriptableBehaviourStateRegistry registry;
        public event Action<Dictionary<string, StateValueBase>> OnChange;
        public class StateValueBase {
            public Type type;
            public event Action OnChange;
            public new virtual string ToString() {
                return "";
            }
            protected void EmitChangeEvent() {
                OnChange?.Invoke();
            }
        }
        public class StateValue<T>: StateValueBase {
            public T value {
                get; protected set;
            } = default(T);

            public StateValue() {
                type = typeof(T);
            }

            public void SetValue(T nextValue) {
                value = nextValue;
                EmitChangeEvent();
            }
            

            public override string ToString() {
                return $"{value}";
            }
        }
        public Dictionary<string, StateValueBase> data = new Dictionary<string, StateValueBase>();

        public StateValue<T> Init<T>(string key) {
            if (!data.ContainsKey(key)) {
                data.Add(key, new StateValue<T>());
                data[key].OnChange += EmitChangeEvent;
            } else {
                if (data[key].type != typeof(T)) {
                    data[key] = new StateValue<T>();
                    data[key].OnChange += EmitChangeEvent;
                }
            }
            return data[key] as StateValue<T>;
        }

        public void Set<T>(string key, T value) {
            if (!data.ContainsKey(key)) {
                data.Add(key, new StateValue<T>());
                data[key].OnChange += EmitChangeEvent;
            } else {
                if (data[key].type != typeof(T)) {
                    data[key] = new StateValue<T>();
                    data[key].OnChange += EmitChangeEvent;
                }
            }
            ((StateValue<T>)data[key]).SetValue(value);
        }

        void EmitChangeEvent() {
            OnChange?.Invoke(data);
        }

        void OnEnable() {
            if (registry != null) {
                registry.Add(this);
            }
            OnBehaviourStateEnable();
        }

        protected virtual void OnBehaviourStateEnable() {

        } 

        void OnDisable() {
            if (registry != null) {
                registry.Remove(this);
            }
            OnBehaviourStateDisable();
        }

        protected virtual void OnBehaviourStateDisable() {

        } 
    }
}