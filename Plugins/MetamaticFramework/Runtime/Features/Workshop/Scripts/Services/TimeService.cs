using System;
using Cysharp.Threading.Tasks;
using Metamatic.SaveSystem;
using UnityEngine;

namespace Metamatic.Framework.Workshop
{
    /// <summary>
    /// Provide time that can be saved and loaded within the game.
    /// </summary>
    public class TimeService: Service, IWorldSavable
    {
        [SerializeField] private float m_DefaultTime = 0f;
        [SerializeField] private float m_DefaultTimeScale = 1f;
        [SerializeField] private float m_DefaultFixedTimeScale = 0.02f;
        
        [Version("1.0.0")]
        public struct SaveData
        {
            public float time;
            public float timeScale;
        }

        private float m_CustomTime = 0f;
        private float m_SystemTimeAtCustomTimeLastChange;
        private float m_TimeScale = 1f;
        
        public float time => m_CustomTime + (Time.time - m_SystemTimeAtCustomTimeLastChange);

        public float timeScale
        {
            get => m_TimeScale;
            set {
                m_TimeScale = value;
                Time.timeScale = value;
            }
        }
        
        public float fixedTimeScale 
        {
            get => Time.fixedDeltaTime;
            set => Time.fixedDeltaTime = value;
        }

        private bool m_WasLoaded;
        private VetoVote m_PauseGameVetoVote = new VetoVote(false);

        /// <summary>
        /// Schedule an action to be executed after a delay.
        /// </summary>
        /// <param name="delay"></param>
        /// <param name="onComplete"></param>
        /// <returns></returns>
        public static DisposableAction Schedule(float delay, Action onComplete, PlayerLoopTiming playerLoopTiming = PlayerLoopTiming.EarlyUpdate)
        {
            var css = SignalSource.Create();
            PlayerLoop.Delay(delay, playerLoopTiming, css.Signal).ContinueWith(onComplete).Forget();
            return DisposableAction.Create(css.Dispose);
        }
        
        public readonly struct ScheduledAsyncAction
        {
            public readonly Action<Signal> OnComplete;
            public readonly Signal CancellationSignal;
            
            public ScheduledAsyncAction(Action<Signal> onComplete, Signal cancellationSignal)
            {
                OnComplete = onComplete;
                CancellationSignal = cancellationSignal;
            }
            
            public void Invoke()
            {
                if (CancellationSignal.isRaised)
                    return;
                OnComplete(CancellationSignal);
            }
        }
        
        /// <summary>
        /// Schedule an cancellable action to be executed after a delay.
        /// </summary>
        /// <param name="delay"></param>
        /// <param name="onComplete"></param>
        /// <param name="playerLoopTiming"></param>
        /// <param name="cancellationSignal"></param>
        public static void Schedule(float delay, Action<Signal> onComplete, PlayerLoopTiming playerLoopTiming = PlayerLoopTiming.EarlyUpdate, Signal cancellationSignal = default)
        {
            var scheduledAction = new ScheduledAsyncAction(onComplete, cancellationSignal);
            PlayerLoop.Delay(delay, playerLoopTiming, cancellationSignal).ContinueWith(scheduledAction.Invoke).Forget();
        }
        
        public void ScheduleAtTime(float time, Action onComplete, Signal cancellationSignal)
        {
            if (time < this.time)
            {
                onComplete();
                return;
            }
            PlayerLoop.Delay(time - this.time, PlayerLoopTiming.EarlyUpdate, cancellationSignal).ContinueWith(onComplete).Forget();
        }
        
        protected override async UniTask RunService(Ready ready, Signal signal)
        {
            if (!m_WasLoaded)
            {
                m_CustomTime = m_DefaultTime;
                m_SystemTimeAtCustomTimeLastChange = Time.time;
                timeScale = m_DefaultTimeScale;
            }

            using var watchPause = m_PauseGameVetoVote.Watch(OnGamePause);
            await ready();
        }

        public DisposableAction Pause()
        {
            return m_PauseGameVetoVote.Vote(true);
        }
        
        /// <summary>
        /// Set the time scale to a new value. Fixed time scale will be adjusted accordingly.
        /// </summary>
        /// <param name="newTimeScale"></param>
        public void SlowDownTime(float newTimeScale)
        {
            // Устанавливаем новый timeScale
            Time.timeScale = newTimeScale;
            // Подгоняем fixedDeltaTime, чтобы фиксированное обновление физики по реальному времени оставалось прежним:
            Time.fixedDeltaTime = m_DefaultFixedTimeScale * newTimeScale;
        }
        
        public void ResetTimeScale() 
        {
            // Возвращаем timeScale к исходному значению
            Time.timeScale = m_DefaultTimeScale;
            // Возвращаем fixedDeltaTime к исходному значению:
            Time.fixedDeltaTime = m_DefaultFixedTimeScale;
        }

        public object SaveToWorld()
        {
            return new SaveData
            {
                time = time,
                timeScale = timeScale
            };
        }

        public UniTask LoadFromWorld(object data)
        {
            var saveData = (SaveData) data;
            m_CustomTime = saveData.time;
            m_SystemTimeAtCustomTimeLastChange = Time.time;
            timeScale = saveData.timeScale;
            m_WasLoaded = true;
            return UniTask.CompletedTask;
        }

        private float m_BeforePauseTimeScale;
        private void OnGamePause(bool paused)
        {
            m_BeforePauseTimeScale = timeScale;
            Time.timeScale = paused ? 0 : m_BeforePauseTimeScale;
        }
    }
}