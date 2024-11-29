namespace Metamatic
{
    using UnityEngine;
    using System;

    public class SpringPulseValue
    {
        private float m_StartValue;
        private readonly float m_Endpoint;
        private float m_StartTime;
        private readonly float m_DecayRate;
        private readonly float m_MinSpeed;
        private readonly TimeSource m_TimeProvider;

        public SpringPulseValue(float startValue, float endpoint, float decayRate, float minSpeed, TimeSource timeProvider = null)
        {
            this.m_StartValue = startValue;
            this.m_Endpoint = endpoint;
            this.m_DecayRate = decayRate;
            this.m_MinSpeed = minSpeed;
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
            float expDecay = Mathf.Exp(-m_DecayRate * t);
            float value = m_Endpoint + (m_StartValue - m_Endpoint) * expDecay;

            // Calculate instantaneous speed
            float speed = Mathf.Abs(m_DecayRate * (m_StartValue - m_Endpoint) * expDecay);
            if (speed < m_MinSpeed)
            {
                // Adjust decay rate to maintain minimum speed
                float adjustedExpDecay = m_MinSpeed * t / Mathf.Abs(m_StartValue - m_Endpoint);
                value = m_Endpoint + (m_StartValue - m_Endpoint) * Mathf.Exp(-adjustedExpDecay);
            }

            return value;
        }
    }
}