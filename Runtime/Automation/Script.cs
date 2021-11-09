using System;
using System.Reflection;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Metamatic{
    public static partial class Automation {
        public class Script {
            public class Config {
                public float defaultDuration = 1f;
                public bool durationIsLocked = false;
                public Automation.OverlapMode overlapMode = Automation.OverlapMode.Unlimit;
                public Automation.TimingMode timingMode = Automation.TimingMode.Autocomplete;
                public bool realTime = false;
            }
            public float defaultDuration {
                get; set;
            } = 1f;
            public bool durationIsLocked {
                get; set;
            } = false;
            public object defaultProps {
                get; set;
            } = null;
            public Type propsType {
                get; set;
            } = null;
            public Automation.OverlapMode overlapMode = Automation.OverlapMode.Unlimit;
            public Automation.TimingMode timingMode = Automation.TimingMode.Autocomplete;
            public bool realTime {
                get; set;
            } = false;

            /* Public events */
            public delegate void ActionRuntime(Automation.Runtime runtime);
            public event ActionRuntime OnRuntimeCreate;
            public event ActionRuntime OnRuntimeRelease;

            /* Internal */
            MethodInfo OnRunDeclaredMethod = null;
            int OnRunParamsCount = 0;

            public Script() {
                TypeInfo scriptTypeInfo = this.GetType().GetTypeInfo();
                /* Invoke script AutomationStart message handler*/
                this.OnRunDeclaredMethod = scriptTypeInfo.GetDeclaredMethod("OnRun");

                if (this.OnRunDeclaredMethod != null) {
                    ParameterInfo[] parameterInfos = OnRunDeclaredMethod.GetParameters();
                    OnRunParamsCount = OnRunDeclaredMethod.GetParameters().Length;
                }
            }

            public Script(Config config) {
                this.defaultDuration = config.defaultDuration;
                this.durationIsLocked = config.durationIsLocked; 
                this.overlapMode = config.overlapMode;
                this.timingMode = config.timingMode;
                this.realTime = config.realTime;
                TypeInfo scriptTypeInfo = this.GetType().GetTypeInfo();
                /* Invoke script AutomationStart message handler*/
                this.OnRunDeclaredMethod = scriptTypeInfo.GetDeclaredMethod("OnRun");

                if (this.OnRunDeclaredMethod != null) {
                    ParameterInfo[] parameterInfos = OnRunDeclaredMethod.GetParameters();
                    OnRunParamsCount = OnRunDeclaredMethod.GetParameters().Length;
                }
            }


            public Automation.Runtime Run(AutomationMonoBehaviour player, float duration, object props = null, CancellationToken cancellationToken = default) {
                /* Izolate propsType in local scope */
                Type propsType = this.propsType;

                /* Determine propsType from defaultProps, if propsType is not specified */
                if (propsType == null && defaultProps != null) {
                    propsType = defaultProps.GetType();
                }

                /* Form props if props in undefined */
                if (props == null) {
                    props = defaultProps;
                }

                /* Assign props to new props, if props has passed as anonym object */
                if (propsType != null && props != null && propsType != props.GetType()) {
                    /* Seems like the props in not of type that should be used for this script, let convert it */
                    var nextProps = defaultProps != null
                        ? defaultProps
                        : Activator.CreateInstance(propsType);
                    props = AssignProps(nextProps, props);
                }

                /* Create local cancellaton token (it will be used to immediate cancel task if runtime will be blocked by overlap mode) */
                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                CancellationTokenRegistration cancellationTokenRegistration;
                
                /* Subscribe to user cancellation token */
                if (cancellationToken != default) {
                    cancellationTokenRegistration = cancellationToken.Register(() => {
                        cancellationTokenSource.Cancel();
                        cancellationTokenRegistration.Dispose();
                    });
                }

                /* Determine duration */
                float finalDuration;
                if (this.durationIsLocked) {
                    /* The script forbade changing the duration */
                    finalDuration = this.defaultDuration;
                } else {
                    finalDuration = duration == default
                        ? this.defaultDuration
                        : duration;
                }

                /* Create runtime */
                Automation.Runtime runtime = new Automation.Runtime(new Automation.Runtime.Config {
                    timingMode = this.timingMode,
                    realTime = this.realTime
                }, finalDuration, cancellationTokenSource.Token);

                /* Subscribe runtime to current behaviour effect lifecycle */
                player.OnUpdate += runtime.Update;
                player.OnFixedUpdate += runtime.FixedUpdate;

                /* Register runtime in player (to make it enable and disable itself) */
                player.RegisterRuntime(runtime);

                /* Emit create event */
                if (OnRuntimeCreate != null) {
                    OnRuntimeCreate(runtime);
                }

                /* Subscribe to runtime complete (finally) */
                runtime.OnFinally += () => {
                    if (this != null) {
                        /* Unsubscribe runtime from current behaviour effect lifecycle */ 
                        player.OnUpdate -= runtime.Update;
                        player.OnFixedUpdate -= runtime.FixedUpdate;

                        /* Play last frame */
                        Action lastFrameHandler = null;
                        lastFrameHandler = () => {
                            player.OnUpdate -= lastFrameHandler;
                            runtime.Release();
                        };
                        player.OnUpdate += lastFrameHandler;

                        /* Emit release event */
                        if (OnRuntimeRelease != null) {
                            OnRuntimeRelease(runtime);
                        }

                        /* Release runtime in player */
                        player.ReleaseRuntime(runtime);

                        /* Dispose cancellationTokenRegistration */
                        if (cancellationTokenRegistration != default) {
                            cancellationTokenRegistration.Dispose();
                        }
                        
                        
                    } else {
                        throw new Exception($"Object '{player.name}' has been destroyed, but runtime stay in progress");
                    }
                };

                /* Start runtime */
                runtime.Start();

                // Action nextFrameAction = null;
                // nextFrameAction = () => {
                //     runtime.OnUpdate -= nextFrameAction;
                    
                // };
                /* Invoke OnRun */
                if (this.OnRunDeclaredMethod != null) {
                    object[] parameters;
                    /* Detect message handler arguments */
                    if (OnRunParamsCount == 0) {
                        parameters = new object[0];
                    } else if (OnRunParamsCount == 1) {
                        parameters = new object[1] {
                            runtime
                        };
                    } else if (OnRunParamsCount == 2) {
                        parameters = new object[2] {
                            runtime,
                            (object)props
                        };
                    } else {
                        throw new Exception("Invalid automation script OnRun attributes count");
                    }
                    OnRunDeclaredMethod.Invoke(this, parameters);
                } else {
                    throw new Exception("Automation script OnRun is not exists");
                }
                
                return runtime;
            }

            protected virtual void OnRun(Automation.Runtime runtime, object props) {
                // Debug.Log("This automation script has no executable bosy script");
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

        public class Script<T>: Script {
            protected virtual void OnRun(Automation.Runtime runtime, T props) {
                // Debug.Log("This automation script has no executable bosy script");
            }
        }
    }
}