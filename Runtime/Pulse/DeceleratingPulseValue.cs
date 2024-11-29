namespace Metamatic
{
    using UnityEngine;
    using System;

    public class DeceleratingPulseValue
    {
        private float m_StartValue;
        private float m_StartTime;
        private readonly float m_InitialVelocity;
        private readonly float m_DecayRate;
        private readonly TimeSource m_TimeProvider;

        public DeceleratingPulseValue(float startValue, float initialVelocity, float decayRate, TimeSource timeProvider = null)
        {
            this.m_StartValue = startValue;
            this.m_InitialVelocity = initialVelocity;
            this.m_DecayRate = decayRate;
            this.m_TimeProvider = timeProvider ?? new TimeSource();
            this.m_StartTime = m_TimeProvider.time;
        }

        public void SetValue(float value)
        {
            this.m_StartValue = value;
            this.m_StartTime = m_TimeProvider.time;
        }

        public float GetValue()
        {
            float t = m_TimeProvider.time - m_StartTime;
            return m_StartValue + (m_InitialVelocity / m_DecayRate) * (1 - Mathf.Exp(-m_DecayRate * t));
        }
    }
}