namespace Metamatic
{
    using UnityEngine;

    /// <summary>
    /// The value decelerates over time, starting from an initial speed, with the rate of change decreasing indefinitely—similar to an object slowing down due to friction.
    /// <example>
    /// var pulse = new DeceleratingPulseValue(0f, 1f, 1f);
    /// </example>
    /// </summary>
    public class DeceleratingPulseValue
    {
        private float m_StartValue;
        private float m_StartTime;
        private float m_InitialVelocity;
        private float m_DecayRate;
        private readonly TimeSource m_TimeProvider;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="startValue"></param>
        /// <param name="initialVelocity">Initial velocity</param>
        /// <param name="decayRate">How fast velocity is slowed down (do not use negative values)</param>
        /// <param name="timeProvider"></param>
        public DeceleratingPulseValue(float startValue, float initialVelocity, float decayRate = 1f, TimeSource timeProvider = null)
        {
            this.m_StartValue = startValue;
            this.m_InitialVelocity = initialVelocity;
            this.m_DecayRate = decayRate;
            this.m_TimeProvider = timeProvider ?? new TimeSource();
            this.m_StartTime = m_TimeProvider.time;
        }
        
        public void SetDecayRate(float decayRate)
        {
            this.m_DecayRate = decayRate;
        }
        
        public void SetInitialVelocity(float initialVelocity)
        {
            this.m_InitialVelocity = initialVelocity;
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