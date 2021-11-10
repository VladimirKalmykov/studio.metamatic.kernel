namespace Metamatic
{
    using System;
    using UnityEngine;
    using Metamatic;
    
    public class GameplayTimeSystem : MonoBehaviour
    {
        private static GameplayTimeSystem instance;
        private float m_RequiredTimeScale = 1f;
        private float m_TransitionDuration = 4f;
        private float m_DefaultTimeScale = 1f;
        private AutomationTrack track;
        private void Awake()
        {
            if (instance != null)
            {
                throw new InvalidOperationException("GameplayTimeSystem can not be instantiated twice");
            }
            instance = this;
        }
    
        public static void SetTime(float timeScale)
        {
            Time.timeScale = timeScale;
        }
    
        public static void ResetTime()
        {
            if (instance != null)
            {
                instance.InstanceResetTime();
            }
            Time.timeScale = 1f;
            Time.fixedDeltaTime = Time.timeScale * 0.02f;
        }
    
        public static void PlayStopEffect()
        {
            if (instance == null)
            {
                Debug.LogError("Can not play stop effect because GameplayTimeSystem's instance is undefined");
                return;
            }
    
            instance.InstancePlayStopEffect();
        }
    
        public void InstanceResetTime()
        {
            if (track != null)
            {
                track.CancelAll();
            }
        }
    
        private void InstancePlayStopEffect()
        {
            track = this.GetAutomationTrack("RealTimeEffects");
            var config = new Automation.Script.Config
            {
                overlapMode = Automation.OverlapMode.Cancel,
                timingMode = Automation.TimingMode.Autocomplete,
                realTime = true
            };
            track.Run(new Automation.FunctionScript(StopEffect, config), m_TransitionDuration);
        }
    
        private void StopEffect(Automation.Runtime runtime)
        {
            runtime.OnUpdate += () =>
            {
                Time.timeScale = Mathf.Lerp(0.2f, 0f, runtime.timer.ratio);
                Time.fixedDeltaTime = Time.timeScale * 0.02f;
            };
            runtime.OnCancel += () =>
            {
                Time.timeScale = m_DefaultTimeScale;
            };
        }
    }

}
