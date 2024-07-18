namespace Metamatic {
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using UnityEngine.Events;

    public static partial class Automation {
        public class Playable: IPlayable {
            public UnityEvent OnBeforePlay { get; private set; } = new UnityEvent();
            public UnityEvent OnAfterPlay { get; private set; } = new UnityEvent();

            Script script;
            AutomationTrack track;
            List<CancellationTokenSource> cancellationTokens = new List<CancellationTokenSource>();
            public int playbackCount {
                get; private set;
            }

            public bool isPlaying {
                get {
                    return playbackCount > 0;
                }
            }

            public Playable(Automation.Script script, AutomationTrack track) {
                this.script = script;
                this.track = track;
                this.track.OnBecomesDisabled += Stop;
            }

            ~Playable() {
                if (track != null) {
                    track.OnBecomesDisabled -= Stop;
                }
            }

            public Task Play() {
                return Play(0f);
            }
            public Task Play(CancellationToken cancellationToken) {
                return Play(0f, null, cancellationToken);
            }

            public Task Play(float duration, object props = null, CancellationToken cancellationToken = default) {
                if (track == null) {
                    var taskSource = new TaskCompletionSource<bool>();
                    taskSource.SetException(new Exception[] { new Exception("Playable script has been detached from the track") });
                    return taskSource.Task;
                }
                var cancellationTokenSource = new CancellationTokenSource();

                CancellationTokenRegistration registration = default;
                
                registration = cancellationToken.Register(() => {
                    cancellationTokenSource.Cancel();
                });
                playbackCount++;
                var runtimeTask = track.Run(script, duration, props, cancellationTokenSource.Token).Task;
                TaskCompletionHandler(runtimeTask, () => {
                    playbackCount--;
                    cancellationTokens.Remove(cancellationTokenSource);
                    cancellationTokenSource.Dispose();
                    registration.Dispose();
                });

                return runtimeTask;
            }

            async void TaskCompletionHandler(Task task, Action handler) {
                try {
                    await task;
                } catch {
                    
                } finally {
                    handler();
                }
            }

            public void Stop() {
                foreach(var cts in cancellationTokens) {
                    cts.Cancel();
                }
            }
        }
    }
}