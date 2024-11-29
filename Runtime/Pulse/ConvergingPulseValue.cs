namespace Metamatic
{
    using UnityEngine;

    /// <summary>
    /// The value starts changing slowly and accelerates as it approaches the endpoint.
    /// </summary>
    public class ConvergingPulseValue
    {
        private float m_StartValue;
        private float m_EndValue;
        private float m_Progress;
        private float m_AttractionForce; // Controls the speed of convergence
        private float m_LastTime;
        private readonly TimeSource m_TimeProvider;

        public ConvergingPulseValue(float startValue, float endValue, float attractionForce, TimeSource timeProvider = null)
        {
            m_StartValue = startValue;
            m_EndValue = endValue;
            m_AttractionForce = Mathf.Abs(attractionForce); // Ensure it's positive
            m_Progress = 0f;
            m_TimeProvider = timeProvider ?? new TimeSource();
            m_LastTime = m_TimeProvider.time;
        }

        public void SetValue(float value)
        {
            m_StartValue = value;
            m_Progress = 0f;
            m_LastTime = m_TimeProvider.time;
        }

        public void SetAttractionForce(float attractionForce)
        {
            m_AttractionForce = Mathf.Abs(attractionForce);
        }

        public float GetValue()
        {
            float currentTime = m_TimeProvider.time;
            float dt = currentTime - m_LastTime;
            m_LastTime = currentTime;

            // Update progress based on attraction force and time elapsed
            m_Progress += m_AttractionForce * dt;
            m_Progress = Mathf.Clamp01(m_Progress); // Ensure progress stays within [0,1]

            // Use a quadratic function to model acceleration
            float easedProgress = m_Progress * m_Progress;

            // Calculate current value
            float currentValue = m_StartValue + (m_EndValue - m_StartValue) * easedProgress;

            return currentValue;
        }
    }
}