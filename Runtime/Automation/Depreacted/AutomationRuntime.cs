using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace Metamatic {
    public class AutomationRuntime: Automation.IRuntime {
        public AutomationBehaviour script {
            get; private set;
        }
        public AutomationTimingMode timingMode {
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
        public CancellationToken cancellationToken {
            get; private set;
        }
        public TimeProgress timer {
            get; private set;
        }
        public bool hasBeenStarted {
            get; private set;
        } = false;
        TaskCompletionSource<object> taskCompletionSource;
        CancellationTokenSource cancellationTokenSource;
        
        public AutomationRuntime(AutomationBehaviour script, AutomationTimingMode timingMode, float duration = -1f, bool realTime = false, CancellationToken outerCancellationToken = default) {
            this.script = script;
            this.timingMode = timingMode;

            /* Apply timing mode to the duration value */
            switch (timingMode) {
                /* Timer should be finished immediatelly */
                case AutomationTimingMode.SideEffect:
                    duration = 0f;
                break;
                /* Timer will never end */
                case AutomationTimingMode.Endless:
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
                        /* TODO Do not need to dispose it? */
                        cancellationTokenRegistration.Dispose();
                    });
                }
            };
        }

        ~AutomationRuntime() {
            this.timer.Stop();
            TryCancel();
        }

        public void Start() {
            hasBeenStarted = true;

            switch (timingMode) {
                case AutomationTimingMode.SideEffect:
                    /* In sideeffect mode task complete immediatelly */
                    Complete();
                break;
                case AutomationTimingMode.Disabled:

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
                if (ex != null) {
                    if (this.OnCancel != null) {
                        this.OnCancel();
                    }
                } else {
                    if (this.OnFault != null) {
                        this.OnFault();
                    }
                }
            } finally {
                if (hasBeenStarted) {
                    /* Stop timer */
                    this.timer.OnComplete -= OnTimerComplete;
                    this.timer.TryStop();
                }

                if (!taskCompletionSource.Task.IsCanceled && !taskCompletionSource.Task.IsFaulted) {
                    /* Invoke complete event */
                    if (this.OnComplete != null) {
                        this.OnComplete();
                    }
                }

                if (this != null) {
                    this.OnFinally();
                }
            }
        }

        private void OnTimerComplete() {
            if (!IsOver) {
                switch (timingMode) {
                    case AutomationTimingMode.Autocomplete:
                        /* Let script call complete before it will be forced */
                        if (OnExpire != null) {
                            OnExpire();
                        }
                        
                        /* If it is still not over */
                        if (!IsOver) {
                            Complete();
                        }
                    break;
                    case AutomationTimingMode.Autocancel:
                        Cancel();
                    break;
                    case AutomationTimingMode.Autothrow:
                        ThrowException(new Exception($"Playback countdown has expired, but script {script.GetType().Name} not complete yet."));
                    break;
                    case AutomationTimingMode.Passive:
                    default:
                        /* Do nothing */
                    break;
                }
            }
        }

        public void Update() {
            if (this.OnUpdate != null) {
                this.OnUpdate();
            }
        }

        public void FixedUpdate() {
            if (this.OnFixedUpdate != null) {
                this.OnFixedUpdate();
            }
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
            throw new Exception("Obsolete");
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
        public int Detain() {
            if (duration > 0) {
                return this.timer.Stop();
            }
            return 0;
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
    }
}