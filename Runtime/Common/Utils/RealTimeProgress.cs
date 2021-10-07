using System.Threading;
using System.Threading.Tasks;
using System;

namespace Metamatic
{
    public class RealTimeProgress {
        public int duration {
            get; private set;
        }
        public DateTime startTime {
            get; private set;
        }
        public float ratio {
            get {
                return GetRatio();
            }
        }
        /// <summary>
        /// Get elapsed time in milliseconds
        /// </summary>
        /// <value></value>
        public int elapsed {
            get {
                return GetElapsedMilliseconds();
            }
        }
        public int remain {
            get {
                return GetRemainMilliseconds();
            }
        }
        public bool completed {
            get {
                return IsCompleted();
            }
        }
        public bool stopped {
            get; private set;
        } = false;
        public CancellationTokenSource cancellationTokenSource {
            get; private set;
        }
        public Action OnComplete;
        public bool isActive {
            get; private set;
        }
        public RealTimeProgress(int duration, bool autoplay = true) {
            this.duration = duration;
            if (autoplay) {
                Start();
            }
            
        }

        public RealTimeProgress(float duration, bool autoplay = true) {
            this.duration = (float.IsInfinity(duration) || duration < 0) ? -1 : (int)(duration * 1000);
            if (autoplay) {
                Start();
            }
        }

        public void Start() {
            startTime = DateTime.Now;
            StartTimeout();
        }

        public float GetRatio() {
            float ratio = (float)((TimeSpan)(DateTime.Now - startTime)).TotalMilliseconds / duration;
            return ratio > 1 ? 1 : ratio;
        }

        public int GetElapsedMilliseconds() {
            return (int)((TimeSpan)(DateTime.Now - startTime)).TotalMilliseconds;
        }

        public int GetRemainMilliseconds() {
            int remain = duration - (int)((TimeSpan)(DateTime.Now - startTime)).TotalMilliseconds;
            return remain >= 0 ? remain : 0;
        }

        public bool IsCompleted() {
            return GetElapsedMilliseconds() >= duration;
        }

        public int Stop() {
            if (cancellationTokenSource != null) {
                cancellationTokenSource.Cancel();
            } else {
                throw new Exception("You can not stop time progress if it's not running");
            }
            return this.elapsed;
        }

        public int TryStop() {
            if (isActive) {
                return Stop();
            }
            return this.elapsed;
        }

        private async void StartTimeout() {
            cancellationTokenSource = new CancellationTokenSource();
            bool cancelled = false;
            Task timerTask = Task.Delay(float.IsInfinity(duration) ? -1 : duration, cancellationTokenSource.Token);

            isActive = true;
            try {
                await timerTask;
            } catch {
                cancelled = true;
            } finally {
                isActive = false;
                cancellationTokenSource = null;
            }
            if (!cancelled && OnComplete != null) {
                OnComplete();
            }
        }
    }
}