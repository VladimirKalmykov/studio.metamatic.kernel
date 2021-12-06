using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Reflection;
using System.Linq;

namespace Metamatic {
    public abstract class AutomationBehaviour: MonoBehaviour {
        public struct OptionalPropertyInfo<T> {
            public string name;
            public T value;
            public bool serializable;
        }
        public const float default_duration = 1f;
        public const bool default_durationIsLocked = false;
        public const AutomationOverlapMode default_overlapMode = AutomationOverlapMode.Cancel;
        public const AutomationTimingMode default_timingMode = AutomationTimingMode.Autocomplete;
        public List<AutomationRuntime> runtimes {
            get; private set;
        } = new List<AutomationRuntime>();
        /// <summary>
        /// Provides action of runtime add
        /// </summary>
        public event Action<AutomationRuntime> OnRuntimeStart;
        /// <summary>
        /// Provides action of runtime remove
        /// </summary>
        public event Action<AutomationRuntime> OnRuntimeEnd;
        public event Action OnUpdate;
        public event Action OnFixedUpdate;
        private float previousTimeScale = 1f;

        /// <summary>
        /// This function is called when the object becomes enabled and active.
        /// </summary>
        void Awake()
        {
            previousTimeScale = Time.timeScale;
            OnBeforeDisable();
            TryDisable();
        }

        protected virtual void OnBeforeDisable() {

        }
        
        void Destroy() {
            /* Cancel all uncompleted runtimes */
            foreach(AutomationRuntime runtime in runtimes) {
                runtime.TryCancel();
            }
        }

        void TryDisable() {
            if (runtimes.Count == 0) {
                this.enabled = false;
            }
        }
        
        public AutomationRuntime Run(AutomationRunConfig runConfig, object props = null, CancellationToken cancellationToken = default) {
            TypeInfo scriptType = this.GetType().GetTypeInfo();
            FieldInfo defaultPropsField = scriptType.GetField("defaultProps");
                var defaultProps = defaultPropsField != null
                    ? defaultPropsField.GetValue(this)
                    : null;

            /* Get default props */
            if (props == null) {
                props = defaultProps;
            }

            Type propsType = defaultPropsField != null
                ? defaultPropsField.GetType()
                : scriptType.GetNestedType("Props");

            if (propsType != null && props != null && propsType != props.GetType()) {
                /* Seems like the props in not of type that should be used for this script, let convert it */
                var nextProps = defaultProps != null
                    ? defaultProps
                    : Activator.CreateInstance(propsType);
                props = AssignProps(nextProps, props);
            }

            /* Get optional properties */
            AutomationTimingMode timingMode = this.GetOptionalPropertyInfo<AutomationTimingMode>("timingMode").value;
            bool durationIsLocked = this.GetOptionalPropertyInfo<bool>("durationIsLocked").value;
            float defaultDuration = this.GetOptionalPropertyInfo<float>("defaultDuration").value;
            bool realTime = this.GetOptionalPropertyInfo<bool>("realTime").value;

            /* Create local cancellaton token (it will be used to immediate cancel task if runtime will be blocked by overlap mode) */
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            CancellationTokenRegistration cancellationTokenRegistration;

            /* Check for already running tasks */
            if (runtimes.Count > 0) {
                AutomationRuntime activeAutomationRuntime = runtimes[runtimes.Count - 1];
                switch (activeAutomationRuntime.script.GetOptionalPropertyInfo<AutomationOverlapMode>("overlapMode").value) {
                    case AutomationOverlapMode.Block:
                        /* Just return cancellated runtime */
                        cancellationTokenSource.Cancel();
                        return new AutomationRuntime(this, timingMode, 0f, false, cancellationTokenSource.Token);
                    case AutomationOverlapMode.Cancel:
                        activeAutomationRuntime.TryCancel();
                    break;
                    case AutomationOverlapMode.Forwards:
                        activeAutomationRuntime.TryComplete();
                    break;
                }
            }
            
            /* Subscribe to user cancellation token */
            if (cancellationToken != default) {
                cancellationTokenRegistration = cancellationToken.Register(() => {
                    cancellationTokenSource.Cancel();
                    cancellationTokenRegistration.Dispose();
                });
            }

            /* Determine duration */
            float duration;
            
            if (durationIsLocked) {
                /* The script forbade changing the duration */
                duration = defaultDuration;
            } else {
                duration = runConfig.duration == default
                    ? defaultDuration
                    : runConfig.duration;
            }

            /* Create runtime */
            AutomationRuntime runtime = new AutomationRuntime(this, timingMode, duration, realTime, cancellationTokenSource.Token);

            /* Add runtime to the runtime list */
            runtimes.Add(runtime);

            /* Emit event */
            if (OnRuntimeStart != null) {
               OnRuntimeStart(runtime);
           }

            /* Subscribe runtime to current behaviour effect lifecycle */
            this.OnUpdate += runtime.Update;
            this.OnFixedUpdate += runtime.FixedUpdate;
            
            /* Enable component on the next lifecycle */
            this.enabled = true;

            string debug_ObjectName = gameObject.name;
            
            runtime.OnFinally += () => {
                if (this != null) {
                    /* Unsubscribe runtime from current behaviour effect lifecycle */ 
                    this.OnUpdate -= runtime.Update;
                    this.OnFixedUpdate -= runtime.FixedUpdate;

                    /* Remove runtime from the runtime list */
                    runtimes.Remove(runtime);

                    TryDisable();

                    /* Emit event */
                    if (OnRuntimeEnd != null) {
                        OnRuntimeEnd(runtime);
                    }
                    

                    /* Dispose cancellationTokenRegistration */
                    if (cancellationTokenRegistration != default) {
                        cancellationTokenRegistration.Dispose();
                    }
                    
                    
                }
            };

            
            runtime.Start();
            
            /* Invoke script AutomationStart message handler*/
            MethodInfo runtimeStartMethod = scriptType.GetDeclaredMethod("AutomationStart");

            if (runtimeStartMethod != null) {
                object[] parameters;
                ParameterInfo[] parameterInfos = runtimeStartMethod.GetParameters();
                int attrsCount = runtimeStartMethod.GetParameters().Count();

                /* Detect message handler arguments */
                if (attrsCount == 0) {
                    parameters = new object[0];
                } else if (attrsCount == 1) {
                    parameters = new object[1] {
                        runtime
                    };
                } else if (attrsCount == 2) {
                    parameters = new object[2] {
                        runtime,
                        (object)props
                    };
                } else {
                    throw new Exception("Invalid automation script OnPlay attributes count");
                }
                runtimeStartMethod.Invoke(this, parameters);
            }

            return runtime;
        }

        /// <summary>
        /// Overload of run to call it with no props
        /// </summary>
        /// <param name="runConfig"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public AutomationRuntime Run(AutomationRunConfig runConfig, CancellationToken cancellationToken = default) {
            return this.Run(runConfig, null, cancellationToken);
        }

        /// <summary>
        /// Run zero-configured automation
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public AutomationRuntime Run(object props, CancellationToken cancellationToken = default) {
            AutomationRunConfig runConfig = new AutomationRunConfig {};
            return this.Run(runConfig, props, cancellationToken);
        }
        
        /// <summary>
        /// Run zero-configured automation
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public AutomationRuntime Run(CancellationToken cancellationToken = default) {
            AutomationRunConfig runConfig = new AutomationRunConfig {};
            return this.Run(runConfig, null, cancellationToken);
        }

        /// <summary>
        /// Return the value of some instance property if it exists. Otherwise it return default value of DEFAULT_[propname] or default for type.
        /// </summary>
        /// <param name="propName"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public OptionalPropertyInfo<T> GetOptionalPropertyInfo<T>(string propName) {
            Type type = this.GetType();
            Type baseType = typeof(AutomationBehaviour);
            PropertyInfo propertyInfo = type.GetProperty(propName, typeof(T));

            if (propertyInfo != null) {
                return new OptionalPropertyInfo<T> {
                    value = (T)propertyInfo.GetValue(this),
                    serializable = false
                };
            }

            FieldInfo fieldInfo = type.GetField(propName);

            if (fieldInfo != null && fieldInfo.FieldType == typeof(T)) {
                var value = fieldInfo.GetValue(this);
                return new OptionalPropertyInfo<T> {
                    name = propName,
                    value = (T)value,
                    serializable = true
                };
            }

            FieldInfo defaultFieldInfo = baseType.GetField($"default_{propName}");
            if (defaultFieldInfo != null && defaultFieldInfo.FieldType == typeof(T)) {
                return new OptionalPropertyInfo<T> {
                    name = propName,
                    value = (T)defaultFieldInfo.GetValue(this),
                    serializable = false
                };
            }

            return new OptionalPropertyInfo<T> {
                    name = propName,
                    value = default(T),
                    serializable = false
                };
        }

        /// <summary>
        /// Return current instance default props (if field defaultProps directly specified in the class, or if class has nested type Props)
        /// </summary>
        public object GetAutomationDefaultProps() {
            Type thisType = this.GetType();
            FieldInfo defaultPropsField = thisType.GetField("defaultProps");
            if (defaultPropsField != null) {
                return defaultPropsField.GetValue(this);
            }
            Type propsType = thisType.GetNestedType("Props");

            if (propsType != null) {
                return Activator.CreateInstance(propsType);
            }

            return null;
        }

        void Update() {
            if (this.OnUpdate != null) {
                OnUpdate();
            }
        }

        /// <summary>
        /// Callback sent to all game objects when the player pauses.
        /// </summary>
        /// <param name="pauseStatus">The pause state of the application.</param>
        void OnApplicationPause(bool pauseStatus)
        {
            
        }

        void FixedUpdate() {
            if (runtimes.Count > 0 && previousTimeScale != Time.timeScale) {
                previousTimeScale = Time.timeScale;

                foreach(AutomationRuntime runtime in runtimes) {
                    /* Tell each runtime timestamp to build elapsed time in previoud timeScale */
                    runtime.MakeTimestamp(previousTimeScale);
                }
            }
            
            if (this.OnFixedUpdate != null) {
                OnFixedUpdate();
            }
        }

        /// <summary>
        /// Assign new values to the props (THIS METHOD IS VERY SLOW)
        /// </summary>
        /// <param name="props"></param>
        /// <param name="newValues"></param>
        /// <returns></returns>
        object AssignProps(object props, object newValues) {
            Type propsType = props.GetType();
            Type newValuesType = newValues.GetType();

            foreach(PropertyInfo propertyInfo in newValuesType.GetProperties()) {
                FieldInfo fieldInfo = propsType.GetField(propertyInfo.Name);
                if (fieldInfo.FieldType == propertyInfo.PropertyType) {
                    fieldInfo.SetValue(props, propertyInfo.GetValue(newValues));
                }
            }
            
            return props;
        }
    }
}