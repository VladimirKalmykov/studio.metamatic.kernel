using System;
using UnityEngine;
using System.Collections.Generic;

namespace Metamatic{
    /// <summary>
    /// This component provides nessessary API for automation will use
    /// </summary>
    public class AutomationMonoBehaviour: MonoBehaviour {
        public delegate void ActionRuntime(Automation.Runtime runtime);
        public event Action OnUpdate;
        public event Action OnFixedUpdate;
        public event Action OnBecomesEnabled;
        public event Action OnBecomesDisabled;
        public event ActionRuntime OnRuntimeAdd;
        public event ActionRuntime OnRuntimeRemove;
        public List<Automation.Runtime> runtimes {
            get; protected set;
        } = new List<Automation.Runtime>();
        public int runtimesCount {
            get {
                return runtimes.Count;
            }
        }

        void OnEnable() {
            OnBecomesEnabled?.Invoke();
        }

        void OnDisable() {
            OnBecomesDisabled?.Invoke();

            /* Cancel all active runtimes */
            foreach(var runtime in runtimes.ToArray()) {
                runtime.TryCancel();
            }
        }

        private float previousTimeScale = 1f;
        void Update() {
            /* Detect Time.timeScale changing */
            if (previousTimeScale != Time.timeScale) {
                previousTimeScale = Time.timeScale;

                foreach(Automation.Runtime runtime in runtimes.ToArray()) {
                    /* Tell each runtime timestamp to build elapsed time in previoud timeScale */
                    runtime.MakeTimestamp(previousTimeScale);
                }
            }
            if (this.OnUpdate != null) {
                OnUpdate();
            }
        }
        
        void FixedUpdate() {
            if (this.OnFixedUpdate != null) {
                OnFixedUpdate();
            }
        }

        public void RegisterRuntime(Automation.Runtime runtime) {
            if (this != null) {
                this.runtimes.Add(runtime);
                RuntimeHasAdded(runtime);
                OnRuntimeAdd?.Invoke(runtime);
                this.enabled = true;
            }
        }

        public void ReleaseRuntime(Automation.Runtime runtime) {
            if (this == null) {
                return;
            }
            this.runtimes.Remove(runtime);
            RuntimeHasRemoved(runtime);
            OnRuntimeRemove?.Invoke(runtime);

            if (runtimesCount == 0) {
                this.enabled = false;
            }

#if (UNITY_EDITOR)
            if (runtimesCount < 0) {
                throw new Exception("Runtime count can not be lower than zero");
            }
#endif
        }

        protected virtual void RuntimeHasRemoved(Automation.Runtime runtime) {}
        protected virtual void RuntimeHasAdded(Automation.Runtime runtime) {
            
        }
    }
}