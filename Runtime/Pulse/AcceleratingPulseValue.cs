namespace Metamatic
{
    using UnityEngine;
    using System;

    /// <summary>
    /// The value accelerates over time, with the rate of change increasing indefinitely—similar to an object accelerating under gravity without terminal velocity.
    /// </summary>
    public class AcceleratingPulseValue
    {
        private float m_StartValue;
        private float m_StartTime;
        private readonly float m_InitialVelocity;
        private float m_Acceleration;
        private readonly TimeSource m_TimeProvider;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="startValue"></param>
        /// <param name="acceleration">How fast it will be accelerated</param>
        /// <param name="initialVelocity">Initial velocity (keeping 0f will give 0.00001f with sign according to acceleration)</param>
        /// <param name="timeProvider"></param>
        public AcceleratingPulseValue(float startValue, float acceleration, float initialVelocity = 0f, TimeSource timeProvider = null)
        {
            m_StartValue = startValue;
            m_InitialVelocity = initialVelocity == 0f ? (0.00001f) * Mathf.Sign(acceleration) : initialVelocity;
            m_Acceleration = acceleration;
            m_TimeProvider = timeProvider ?? new TimeSource();
            m_StartTime = m_TimeProvider.time;
        }
        
        public void SetAcceleration(float acceleration)
        {
            m_Acceleration = acceleration;
        }

        public void SetValue(float value)
        {
            m_StartValue = value;
            m_StartTime = m_TimeProvider.time;
        }

        public float GetValue()
        {
            float t = m_TimeProvider.time - m_StartTime;
            return m_StartValue + m_InitialVelocity * t + 0.5f * m_Acceleration * t * t;
        }
    }
}