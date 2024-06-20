using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Metamatic{
    public static partial class Automation {
        public class Runtime: Automation.IRuntime {
            public class Range {
                public event Action OnStart;
                public event Action<float> OnUpdate;
                public event Action OnEnd;
                bool hasStarted;
                public Range(Runtime runtime, float fromTime, float toTime) {
                    if (fromTime >= toTime) {
                        throw new Exception("Start time can be grater or equal to end time");
                    }
                    var startTimeMs = (int)(fromTime * 1000f);
                    var endTimeMs = (int)(toTime * 1000f);
                    float totalDuration = endTimeMs - startTimeMs;
                    Action handler = null;
                    handler = () => {
                        if (runtime.timer.elapsed >= startTimeMs && runtime.timer.elapsed <= endTimeMs) {
                            if (!hasStarted) {
                                hasStarted = true;
                                OnStart?.Invoke();
                            }
                            OnUpdate?.Invoke((runtime.timer.elapsed - startTimeMs) / totalDuration);
                        } else if (hasStarted) {
                            OnEnd?.Invoke();
                            runtime.OnUpdate -= handler;
                        }
                    };
                    runtime.OnUpdate += handler;
                }

                public Range Start(Action handler) {
                    OnStart += handler;
                    return this;
                }

                public Range Update(Action<float> handler) {
                    OnUpdate += handler;
                    return this;
                }

                public Range End(Action handler) {
                    OnEnd += handler;
                    return this;
                }
            }
            public struct Config {
                public Automation.TimingMode timingMode;
                public bool realTime;
            }
            public Automation.TimingMode timingMode {
                get; private set;
            }
            public float duration {
                get; private set;
            }
            public int durationMilliseconds {
                get {
                    return (int)(duration * 1000);
                }
            }
            public Task<object> Task {
                get {
                    return taskCompletionSource.Task;
                }
            }
            /// <summary>
            ///  Determine Playback is totally over (completed, canceled or thrown)
            /// </summary>
            /// <value></value>
            public bool IsOver {
                get {
                    return this.taskCompletionSource.Task.IsCompleted
                        || this.taskCompletionSource.Task.IsCanceled
                        || this.taskCompletionSource.Task.IsFaulted;
                }
            }
            public bool IsCompleted {
                get {
                    return this.taskCompletionSource.Task.IsCompleted;
                }
            }
            public bool IsCancellationRequested {
                get {
                    return cancellationToken.IsCancellationRequested;
                }
            }
            public bool IsCancelled {
                get {
                    return this.taskCompletionSource.Task.IsCanceled;
                }
            }
            public bool IsFaulted {
                get {
                    return this.taskCompletionSource.Task.IsFaulted;
                }
            }
            public event Action OnFinally;
            public event Action OnCancel;
            public event Action OnComplete;
            public event Action OnExpire;
            public event Action OnFault;
            public event Action OnUpdate;
            public event Action OnFixedUpdate;
            public event Action OnRelease;
            public CancellationToken cancellationToken {
                get; private set;
            }
            public TimeProgress timer {
                get; private set;
            }
            public bool hasBeenStarted {
                get; private set;
            }
            TaskCompletionSource<object> taskCompletionSource;
            CancellationTokenSource cancellationTokenSource;
            private Config m_Config;
            
            public Runtime(Config config, float duration = -1f, CancellationToken outerCancellationToken = default)
            {
                m_Config = config;
                var timingMode = config.timingMode;
                var realTime = config.realTime;

                this.timingMode = timingMode;

                /* Apply timing mode to the duration value */
                switch (timingMode) {
                    /* Timer should be finished immediatelly */
                    case Automation.TimingMode.SideEffect:
                        duration = 0f;
                    break;
                    /* Timer will never end */
                    case Automation.TimingMode.Endless:
                        duration = float.PositiveInfinity;
                    break;
                }

                this.duration = duration;

                taskCompletionSource = new TaskCompletionSource<object>();
                cancellationTokenSource = new CancellationTokenSource();
                cancellationToken = cancellationTokenSource.Token;

                /* Subscribe to own cancellation token */
                cancellationToken.Register(() => {
                    cancellationTokenSource.Dispose();
                    Cancel();
                });

                /* Initialize timer */
                this.timer = new TimeProgress(duration, false, realTime);

                /* Wait fot task complete */
                this.WaitForTask();

                /* Subscrive to user cancellation token */
                if (outerCancellationToken != default) {
                    if (outerCancellationToken.IsCancellationRequested) {
                        cancellationTokenSource.Cancel();

                        /* Exit function */
                        return;
                    } else
                    {
                        CancellationTokenRegistration cancellationTokenRegistration = default;
                        cancellationTokenRegistration = outerCancellationToken.Register(() => {
                            cancellationTokenSource.Cancel();
                            /* TODO Need to dispose? */
                            cancellationTokenRegistration.Dispose();
                        });
                    }
                };
            }

            ~Runtime() {
                this.timer.Stop();
                TryCancel();
            }

            public void Start() {
                hasBeenStarted = true;

                switch (timingMode) {
                    case Automation.TimingMode.SideEffect:
                        /* In sideeffect mode task complete immediatelly */
                        Complete();
                    break;
                    case Automation.TimingMode.Disabled:

                    break;
                    default:
                        this.timer.OnComplete += OnTimerComplete;
                        this.timer.Start();
                    break;
                }
            }

            async void WaitForTask() {
                try {
                    await taskCompletionSource.Task;
                } catch(TaskCanceledException ex) {
                    /* Invoke cancel event */
                    if (ex != null)
                    {
                        this.OnCancel?.Invoke();
                    } else
                    {
                        this.OnFault?.Invoke();
                    }
                } finally {
                    if (hasBeenStarted) {
                        /* Stop timer */
                        this.timer.OnComplete -= OnTimerComplete;
                        this.timer.TryStop();
                    }

                    if (!taskCompletionSource.Task.IsCanceled && !taskCompletionSource.Task.IsFaulted)
                    {
                        /* Invoke complete event */
                        this.OnComplete?.Invoke();
                    }

                    if (this != null) {
                        this.OnFinally?.Invoke();
                    }
                }
            }

            private void OnTimerComplete()
            {
                if (IsOver) return;
                switch (timingMode) {
                    case Automation.TimingMode.Autocomplete:
                        /* Let script call complete before it will be forced */
                        OnExpire?.Invoke();

                        /* If it is still not over */
                        if (!IsOver) {
                            Complete();
                        }
                        break;
                    case Automation.TimingMode.Autocancel:
                        Cancel();
                        break;
                    case Automation.TimingMode.Autothrow:
                        ThrowException(new Exception($"Playback countdown has expired, but script not complete yet."));
                        break;
                    case Automation.TimingMode.Passive:
                    default:
                        /* Do nothing */
                        break;
                }
            }

            public void Update()
            {
                this.OnUpdate?.Invoke();
            }

            public void FixedUpdate()
            {
                this.OnFixedUpdate?.Invoke();
            }

            public void Release() {
                OnRelease?.Invoke();
            }

            public void Complete() {
                if (timer.isActive) {
                    timer.Stop();
                }
                taskCompletionSource.SetResult(null);
            }

            public void SetResult(object result) {
                timer.TryStop();
                taskCompletionSource.SetResult(result);
            }

            public void TryComplete() {
                if (!this.IsOver) {
                    taskCompletionSource.SetResult(null);
                }
            }

            /// <summary>
            /// Wait remain time of the effect progress
            /// </summary>
            /// <returns></returns>
            public Task Wait() {
                throw new Exception("Wait is deprecated");
            }

            /// <summary>
            /// Cancel Playback
            /// </summary>
            public void Cancel() {
                timer.TryStop();
                this.taskCompletionSource.SetCanceled();
            }

            /// <summary>
            /// Cancel Playback if it is not over yet
            /// </summary>
            public void TryCancel() {
                if (!this.IsOver) {
                    this.taskCompletionSource.SetCanceled();
                }
            }

            /// <summary>
            /// Throw Playback exception
            /// </summary>
            /// <param name="exception"></param>
            public void ThrowException(Exception exception) {
                this.taskCompletionSource.SetException(exception);
            }

            /// <summary>
            /// Throw Playback exception if it is not over yet
            /// </summary>
            /// <param name="exception"></param>
            public void TryThrowException(Exception exception) {
                if (!this.IsOver) {
                    this.taskCompletionSource.SetException(exception);
                }
            }

            /// <summary>
            /// Disable autocompletion (turn off сountdown)
            /// </summary>
            /// <returns></returns>
            public int Detain()
            {
                return duration > 0 ? this.timer.Stop() : 0;
            }

            public TaskAwaiter<object> GetAwaiter() {
                return Task.GetAwaiter();
            }

            protected Dictionary<string, object> stash = new Dictionary<string, object>();

            public void Stash(string name, object value) {
                stash.Add(name, value);
            }

            public T Stash<T>(string name) {
                if (!stash.ContainsKey(name)) {
                    return default(T);
                }
                return (T)stash[name];
            }

            public bool HasStash<T>(string name) {
                if (!stash.ContainsKey(name)) {
                    return false;   
                }
                var value = stash[name];
                return value.GetType() == typeof(T);
            }
            /// <summary>
            /// This method should be called each time before Time.timeScale will change
            /// </summary>
            public void MakeTimestamp(float timeScale) {
                this.timer.MakeTimestamp(timeScale);
            }
            
            public void EveryMilliseconds(int ms, Action handler) {
                var seconds = (float)ms / 1000f;
                var startTime = m_Config.realTime ? Time.unscaledTime : Time.time;
                this.OnUpdate += () =>
                {
                    var time = m_Config.realTime ? Time.unscaledTime : Time.time;
                    if (!(time - startTime >= seconds)) return;
                    var surplus = time - startTime - seconds;
                    startTime = time + surplus;
                    handler();
                };
            }

            public void EveryRealtimeMilliseconds(int ms, Action handler) {
                var seconds = (float)ms / 1000f;
                var startTime = Time.unscaledTime;
                this.OnUpdate += () =>
                {
                    if (!(Time.unscaledTime - startTime >= seconds)) return;
                    var surplus = Time.unscaledTime - startTime - seconds;
                    startTime = Time.unscaledTime + surplus;
                    handler();
                };
            }

            /// <summary>
            /// Specify handler for time range
            /// </summary>
            /// <param name="from"></param>
            /// <param name="to"></param>
            /// <param name="handler"></param>
            public Range CreateRange(float from, float to, Action<float> handler = null) {
                if (this.duration == 0f) {
                    throw new Exception("You can not select runtime range for infinity duration");
                }
                
                var runtimeRange = new Runtime.Range(this, from * duration, to * duration);
                
                if (handler != null) {
                    runtimeRange.Update(handler);
                }

                return runtimeRange;
            }

            /// <summary>
            /// Specify handler for time range
            /// </summary>
            /// <param name="from">From time</param>
            /// <param name="to">To time</param>
            /// <param name="handler"></param>
            public Range CreateTimeRange(float from, float to) {
                if (this.duration == 0f) {
                    throw new Exception("You can not select runtime range for infinity duration");
                }
                return new Runtime.Range(this, from, to);
            }
        }
    }
}