namespace Metamatic
{
    using UnityEngine;
    using System;

    public class ConvergingPulseValue
    {
        private float m_StartValue;
        private readonly float m_Endpoint;
        private float m_StartTime;
        private float m_Rate;
        private readonly TimeSource m_TimeProvider;

        public ConvergingPulseValue(float startValue, float endpoint, float rate, TimeSource timeProvider = null)
        {
            this.m_StartValue = startValue;
            this.m_Endpoint = endpoint;
            this.m_Rate = rate;
            this.m_TimeProvider = timeProvider ?? new TimeSource();
            this.m_StartTime = m_TimeProvider.time;
        }
        
        public void SetRate(float rate)
        {
            this.m_Rate = rate;
        }

        public void SetValue(float value)
        {
            this.m_StartValue = value;
            this.m_StartTime = m_TimeProvider.time;
        }

        public float GetValue()
        {
            float t = m_TimeProvider.time - m_StartTime;
            float expDecay = Mathf.Exp(-m_Rate * t * t);
            return m_Endpoint - (m_Endpoint - m_StartValue) * expDecay;
        }
    }
}