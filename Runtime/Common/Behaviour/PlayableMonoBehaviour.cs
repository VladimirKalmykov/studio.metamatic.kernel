namespace Metamatic {
    using System.Collections;
    using UnityEngine;
    using System.Threading;
    using System.Threading.Tasks;
    using UnityEngine.Events;
    using Sirenix.OdinInspector;

    /// <summary>
    /// Standart mono behaviour component that implements interface IPlayable
    /// In shorts, you can always call PLay method of this object, even if it have not implemented
    /// In such way all playable behaviours have the same play entry method
    /// </summary>
    public abstract class PlayableMonoBehaviour: MonoBehaviour, IPlayable {
        public UnityEvent OnBeforePlay {
            get; private set;
        } = new UnityEvent();
        public UnityEvent OnAfterPlay {
            get; private set;
        } = new UnityEvent();
        private CancellationTokenSource cancellationTokenSource;

        [Button("Play")]
        public void PlayForCallback() {
            Play();
        }
        public Task Play() {
            var cancellationTokenSource = new CancellationTokenSource();
            return Play(cancellationTokenSource.Token);
        }
        
        public Task Play(CancellationToken userCancellationToken) {
            cancellationTokenSource = new CancellationTokenSource();
            CancellationTokenRegistration registration = default;
            registration = userCancellationToken.Register(() => {
                cancellationTokenSource.Cancel();
                registration.Dispose();
            });
            OnBeforePlay.Invoke();
            Task task = OnPlay(cancellationTokenSource.Token);

            if (this.isActiveAndEnabled) {
                StartCoroutine(InvokeAfterPlay());
            }

            return task;
        }

        [Button("Stop")]
        /// <summary>
        /// Stop playback
        /// </summary>
        public void Stop() {
            if (cancellationTokenSource != null && !cancellationTokenSource.IsCancellationRequested) {
                cancellationTokenSource.Cancel();
            }
        }

        protected virtual Task OnPlay(CancellationToken cancellationToken) {
            var taskCompletionSource = new TaskCompletionSource<bool>();
            if (cancellationToken.IsCancellationRequested) {
                taskCompletionSource.SetCanceled();
            } else {
                taskCompletionSource.TrySetResult(true);
            }

            return taskCompletionSource.Task;
        }

        protected virtual void AfterPlay() {}

        private IEnumerator InvokeAfterPlay() {
            yield return new WaitForEndOfFrame();
            AfterPlay();
            OnAfterPlay.Invoke();
        }
    }
}