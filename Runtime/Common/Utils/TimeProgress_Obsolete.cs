using UnityEngine;
using System.Threading;
using System.Threading.Tasks;
using System;

namespace Metamatic
{
    /// <summary>
    /// Counts miliseconds since instance has been created. It takes in opinion Time.timeScale each time you ask for milliseconds. So if you known that timeScale will be changed, you should ask for milliseconds or call MakeTimestamp() method to create timestamp for current timeScale. Thus, even if the timeScale is changed several times during the counting process, the final millisecond count will be correct and will take into account all changes in the timeline during the calculation.
    /// </summary>
    public class ScaledTime {
        /// <summary>
        /// Time, since it create timestamp
        /// </summary>
        private DateTime m_Timestamp;
        private double m_elapsedMilliseconds;
        /// <summary>
        /// Determines that it is real time or scaled
        /// </summary>
        private bool realTime;
        /// <summary>
        /// Return milliseconds since SclaedTime was created. It also takes in opinion timeScale.
        /// </summary>
        /// <value></value>
        public int milliseconds {
            get {
                MakeTimestamp(realTime ? 1f : Time.timeScale);
                return (int)m_elapsedMilliseconds;
            }
        }
        public ScaledTime(bool realTime = false) {
            this.realTime = realTime;
            m_Timestamp = DateTime.Now;
        }
        /// <summary>
        /// Reset elapsed milliseconds (just like start over)
        /// </summary>
        public void Reset() {
            m_Timestamp = DateTime.Now;
            m_elapsedMilliseconds = 0;
        }
        public void MakeTimestamp(float timeScale) {
            m_elapsedMilliseconds += (DateTime.Now - m_Timestamp).TotalMilliseconds * (realTime ? 1f : timeScale);
            m_Timestamp = DateTime.Now;
        }
    }
    [Obsolete("This class is deprecated, use UniTask.Delay instead", true)]
    public class TimeProgress {
        enum TechnicalCancellationPurpose {
            Replay
        }
        /// <summary>
        /// Same as Tadk.Delay, but вшмшвув ин Time.timeScale
        /// </summary>
        /// <param name="ms"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public static Task Delay(int ms, CancellationToken token = default) {
            int time = Time.timeScale != 0 ? (int)(ms / Time.timeScale) : -1;
            return Task.Delay(time, token);
        }
        private ScaledTime m_ScaledTime;
        public int duration {
            get; private set;
        }
        public int startTime {
            get; private set;
        }
        public DateTime startRealTime {
            get; private set;
        }
        public float unityStartTime {
            get; private set;
        }
        public float ratio {
            get {
                return GetRatio();
            }
        }
        public float realTimeRatio {
            get {
                return GetRealTimeRatio();
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
        public Action OnStopped;
        public Action OnFinally;
        public bool isActive {
            get; private set;
        }
        public bool realTime {
            get; private set;
        }
        bool m_TechnicalCancellation;
        TechnicalCancellationPurpose m_TechnicalCancellationPurpose;
        Task timerTask;
        public Task task {
            get {
                return timerTask;
            }
        }
        public TimeProgress(int duration, bool autoplay = true, bool realTime = false) {
            Construct(duration, autoplay, realTime);
        }

        public TimeProgress(float duration, bool autoplay = true, bool realTime = false) {
            int durationMs = (float.IsInfinity(duration) || duration < 0) ? -1 : (int)(duration * 1000);
            Construct(durationMs, autoplay, realTime);
        }

        void Construct(int duration, bool autoplay = true, bool realTime = false) {
            m_ScaledTime = new ScaledTime(realTime);
            this.duration = duration;
            this.realTime = realTime;
            if (autoplay) {
                Start();
            }
        }

        public void Start() {
            if (!isActive) {
                startTime = m_ScaledTime.milliseconds;
                startRealTime = DateTime.Now;
                unityStartTime = Time.time;
                StartTimeout();
            } else {
                throw new Exception("Timer is already played");
            }
        }

        public float GetRatio() {
            float elapsedMilliseconds = GetElapsedMilliseconds();
            float ratio = elapsedMilliseconds / duration;
            return ratio > 1 ? 1 : ratio;
        }

        public float GetRealTimeRatio() {
           float ratio = (float)((TimeSpan)(DateTime.Now - startRealTime)).TotalMilliseconds / duration;
           return ratio > 1 ? 1 : ratio;
        }

        public int GetElapsedMilliseconds() {
            return m_ScaledTime.milliseconds - startTime;
        }

        public int GetRemainMilliseconds() {
            int remain = duration - GetElapsedMilliseconds();
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

        /// <summary>
        /// Try to stop without exception in the case of an unactive timer
        /// </summary>
        /// <returns></returns>
        public int TryStop() {
            if (isActive) {
                return Stop();
            }
            return this.elapsed;
        }

        /// <summary>
        /// Reset time progress (just like start timer again, but without stop event)
        /// </summary>
        public void Restart() {
            m_ScaledTime.Reset();
            if (isActive) {
                m_TechnicalCancellation = true;
                m_TechnicalCancellationPurpose = TechnicalCancellationPurpose.Replay;
                cancellationTokenSource.Cancel();
            } else {
                Start();
            }
        }
        
        private async void StartTimeout() {
            cancellationTokenSource = MiscUtils.Renew(cancellationTokenSource);
            bool cancelled = false;
            float elapsed = GetElapsedMilliseconds();

            if (elapsed < duration) {
                timerTask = this.realTime
                ? Task.Delay(float.IsInfinity(duration) ? -1 : duration - GetElapsedMilliseconds(), cancellationTokenSource.Token)
                : Delay(float.IsInfinity(duration) ? -1 : duration - GetElapsedMilliseconds(), cancellationTokenSource.Token);
            } else {
                TaskCompletionSource<object> taskCompletionSource = new TaskCompletionSource<object>();
                timerTask = taskCompletionSource.Task;
                taskCompletionSource.SetResult(null);
            }
            

            isActive = true;
            try {
                await timerTask;
            } catch {
                if (!m_TechnicalCancellation) {
                    cancelled = true;

                    if (OnStopped != null) {
                        OnStopped();
                    }
                }
            } finally {
                isActive = false;
            }
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = null;
            if (m_TechnicalCancellation) {
                m_TechnicalCancellation = false;
                switch(m_TechnicalCancellationPurpose) {
                    case TechnicalCancellationPurpose.Replay:
                        /* It was technical calculation, timer should be recreated */
                        StartTimeout();
                    break;
                    default:
                        throw new Exception("Unsupported timeProgress behaviour");
                }
                return;
            } else if (!cancelled && OnComplete != null) {
                OnComplete();
            }

            if (OnFinally != null) {
                OnFinally();
            }
        }

        /// <summary>
        /// Make timestamp and recalculate remained time relative to the current timeScale
        /// </summary>
        public void MakeTimestamp(float previousTimeScale) {
            if (cancellationTokenSource != null) {
                m_TechnicalCancellation = true;
                m_TechnicalCancellationPurpose = TechnicalCancellationPurpose.Replay;
                m_ScaledTime.MakeTimestamp(previousTimeScale);
                cancellationTokenSource.Cancel();
            }
        }
    }
}