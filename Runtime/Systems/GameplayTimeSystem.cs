namespace Metamatic
{
    using System;
    using UnityEngine;

    public class GameplayTimeSystem : MonoBehaviour
    {
        private static GameplayTimeSystem m_Instance;
        private const float TRANSITION_DURATION = 4f;
        private const float DEFAULT_TIME_SCALE = 1f;
        private AutomationTrack m_Track;
        private void Awake()
        {
            if (m_Instance != null)
            {
                throw new InvalidOperationException("GameplayTimeSystem can not be instantiated twice");
            }
            m_Instance = this;
        }
    
        public static void SetTime(float timeScale)
        {
            Time.timeScale = timeScale;
        }
    
        public static void ResetTime()
        {
            if (m_Instance != null)
            {
                m_Instance.InstanceResetTime();
            }
            Time.timeScale = 1f;
            Time.fixedDeltaTime = Time.timeScale * 0.02f;
        }
    
        public static void PlayStopEffect()
        {
            if (m_Instance == null)
            {
                Debug.LogError("Can not play stop effect because GameplayTimeSystem's instance is undefined");
                return;
            }
    
            m_Instance.InstancePlayStopEffect();
        }
    
        public void InstanceResetTime()
        {
            if (m_Track != null)
            {
                m_Track.CancelAll();
            }
        }
    
        private void InstancePlayStopEffect()
        {
            m_Track = this.GetAutomationTrack("RealTimeEffects");
            var config = new Automation.Script.Config
            {
                overlapMode = Automation.OverlapMode.Cancel,
                timingMode = Automation.TimingMode.Autocomplete,
                realTime = true
            };
            m_Track.Run(new Automation.FunctionScript(StopEffect, config), TRANSITION_DURATION);
        }
    
        private static void StopEffect(Automation.Runtime runtime)
        {
            runtime.OnUpdate += () =>
            {
                Time.timeScale = Mathf.Lerp(0.2f, 0f, runtime.timer.ratio);
                Time.fixedDeltaTime = Time.timeScale * 0.02f;
            };
            runtime.OnCancel += () =>
            {
                Time.timeScale = DEFAULT_TIME_SCALE;
            };
        }
    }

}
