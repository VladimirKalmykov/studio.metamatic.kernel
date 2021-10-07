using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sirenix.OdinInspector;

namespace Metamatic {
    public class AutomationTrack: AutomationMonoBehaviour, ITaskDriven {
        public struct QueueItem {
            public Automation.Script script;
            public float duration;
            public object props;
            public CancellationToken cancellationToken;
            public TaskCompletionSource<bool> taskCompletionSource;
        }
        [GUIColor("GetNameFieldColor")]
        public new string name = "Default";
        #if UNITY_EDITOR
        Color GetNameFieldColor() {
            Sirenix.Utilities.Editor.GUIHelper.RequestRepaint();
            return name != "Default"
                ? new Color(0.3f, 0.8f, 0.8f, 1f)
                : Color.gray;
        }
        #endif
        [FoldoutGroup("Playback config")]
        public bool fixedOverlapMode = false;
        [ShowIf("fixedOverlapMode"), Tooltip("Determines multiple automation playback behaviour")]
        [FoldoutGroup("Playback config")]
        public Automation.OverlapMode overlapMode = Automation.OverlapMode.Unlimit;
        public readonly Automation.FunctionScript EMPTY_SCRIPT = new Automation.FunctionScript((Automation.Runtime runtime) => {});
        private Automation.Script.Config defaultConfig;
        private List<QueueItem> queue = new List<QueueItem>();
        /// <summary>
        /// Specify fixed overlap mode for this track (even if script config has another overlapMode config it will be ignored)
        /// </summary>
        /// <param name="overlapMode"></param>
        public void SetOverlapMode(Automation.OverlapMode overlapMode) {
            this.fixedOverlapMode = true;
            this.overlapMode = overlapMode;
        }
        public void SetDefaultConfig(Automation.Script.Config config) {
            defaultConfig = config;
        }
        public Automation.Runtime Run(float duration = 0f, CancellationToken cancellationToken = default) {
            return this.Run(EMPTY_SCRIPT, duration, cancellationToken);
        }

        public Automation.Runtime Run(Automation.FunctionScript.AsyncFunction script, float duration = 0f, CancellationToken cancellationToken = default) {
            if (defaultConfig == null) {
                return this.Run(new Automation.FunctionScript(script), duration, cancellationToken);;
            }
            return this.Run(new Automation.FunctionScript(script, defaultConfig), duration, cancellationToken);
        }

        public Automation.Runtime Run(Automation.FunctionScript.ScriptFunction script, float duration = 0f, CancellationToken cancellationToken = default) {
            var automationScript = defaultConfig != null
                ? new Automation.FunctionScript(script, defaultConfig)
                : new Automation.FunctionScript(script);
            return this.Run(automationScript, duration, cancellationToken);
        }

        /// <summary>
        /// Run script without props
        /// </summary>
        /// <param name="script"></param>
        /// <param name="duration"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Automation.Runtime Run(Automation.Script script, float duration, CancellationToken cancellationToken) {
            return this.Run(script, duration, null, cancellationToken);
        }

        public Automation.Runtime Run<T>(float duration = 0f, CancellationToken cancellationToken = default) where T: Automation.Script, new()  {
            var script = new T();
            return Run(script, duration, cancellationToken);
        }

        public Automation.Runtime Run<T>(object props, float duration = 0f, CancellationToken cancellationToken = default) where T: Automation.Script, new()  {
            var script = new T();
            return Run(script, duration, props, cancellationToken);
        }

        public Automation.Runtime Run(Automation.Script script, float duration = 0f, object props = null, CancellationToken cancellationToken = default) {
            /* Create local cancellaton token (it will be used to immediate cancel task if runtime will be blocked by overlap mode) */
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            CancellationTokenRegistration cancellationTokenRegistration;

            /* Check for already running tasks */
            if (runtimes.Count > 0) {
                Automation.Runtime activeAutomationRuntime = runtimes[runtimes.Count - 1];
                var currentOverlapMode = fixedOverlapMode ? overlapMode : script.overlapMode;
                switch (currentOverlapMode) {
                    case Automation.OverlapMode.Block:
                        /* Just return cancellated runtime */
                        cancellationTokenSource.Cancel();
                        return new Automation.Runtime(new Automation.Runtime.Config {
                            timingMode = script.timingMode
                        }, 0f, cancellationTokenSource.Token);
                    case Automation.OverlapMode.Cancel:
                        activeAutomationRuntime.TryCancel();
                    break;
                    case Automation.OverlapMode.Forwards:
                        activeAutomationRuntime.TryComplete();
                    break;
                    /* Add next script to the queue */
                    case Automation.OverlapMode.Sequence:
                        throw new Exception("Automation.OverlapMode.Sequence is obsolete");
                }
            }
            
            /* Subscribe to user cancellation token */
            if (cancellationToken != default) {
                cancellationTokenRegistration = cancellationToken.Register(() => {
                    cancellationTokenSource.Cancel();
                    cancellationTokenRegistration.Dispose();
                });
            }

            Automation.Runtime runtime = script.Run(this, duration, props, cancellationToken);
            
            /* Add runtime to the runtime list */
            // runtimes.Add(runtime); // Now it do Script class

            Action OnFinally = null;
            
            OnFinally = () => {
                runtime.OnFinally -= OnFinally;

                /* Dispose cancellation registration */
                cancellationTokenRegistration.Dispose();
                
                /* Remove runtime from the runtime list */
                // runtimes.Remove(runtime); // Now it happends in Script class
            };
            runtime.OnFinally += OnFinally;

            return runtime;
        }

        protected override void RuntimeHasRemoved(Automation.Runtime runtime) {
            /* Run script from the queue*/
            if (runtimes.Count == 0) {
                RunFromQueue();
            }
        }

        async void RunFromQueue() {
            if (queue.Count > 0) {
                var item = queue[0];
                queue.RemoveAt(0);
                try {
                    await Run(item.script, item.duration, item.props, item.cancellationToken);
                    item.taskCompletionSource.SetResult(true);
                } catch (OperationCanceledException) {
                    item.taskCompletionSource.SetCanceled();
                } catch (Exception e) {
                    item.taskCompletionSource.SetException(e);
                }
            }
        }
        
        /// <summary>
        /// Returns task
        /// </summary>
        /// <returns></returns>
        public Task GetTask() {
            if (runtimes.Count > 0) {
                return Task.WhenAll(runtimes.Select(runtimes => runtimes.Task));
            }

            return Task.FromResult(true);
        }

        [Obsolete]
        /// <summary>
        /// Cancel current sequences
        /// </summary>
        public void Cancel() {
            foreach(var runtime in runtimes.ToArray()) {
                runtime.TryCancel();
            }
        }

         /// <summary>
        /// Cancel current sequences
        /// </summary>
        public void CancelAll() {
            foreach(var runtime in runtimes.ToArray()) {
                runtime.TryCancel();
            }
        }
        
        /// <summary>
        /// Prepare automation script to use standalone
        /// </summary>
        /// <param name="script"></param>
        /// <returns></returns>
        public Automation.Playable CreatePlayable(Automation.Script script) {
            return new Automation.Playable(script, this);
        }
    }
}