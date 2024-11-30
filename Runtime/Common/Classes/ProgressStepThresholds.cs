using System;
using System.Collections.Generic;
using UnityEngine;

namespace Metamatic
{
    public class ProgressStepThresholds
    {
        private readonly Action<float> m_Callback;
        private readonly List<float> m_Thresholds;
        private readonly Func<float> m_GetRevokeDuration;
        private float m_PreviousValue = float.MinValue;
        private float m_NextRevokeTime = float.MaxValue;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="callback">Will be called with threshold is reached</param>
        /// <param name="thresholds">Array of thresholds</param>
        /// <param name="getRevokeDuration">Get duration after which previous value will be revoked and thresholds become reachable again</param>
        /// <exception cref="ArgumentNullException"></exception>
        public ProgressStepThresholds(Action<float> callback, IEnumerable<float> thresholds, Func<float> getRevokeDuration = null)
        {
            m_Callback = callback ?? throw new ArgumentNullException(nameof(callback));
            m_Thresholds = new List<float>(thresholds);
            m_Thresholds.Sort();
            m_GetRevokeDuration = getRevokeDuration;
        }

        public void Set(float value)
        {
            if (Time.time >= m_NextRevokeTime)
            {
                m_PreviousValue = float.MinValue;
            }
            for (var i = 0; i < m_Thresholds.Count; i++)
            {
                if (value >= m_Thresholds[i] && m_PreviousValue < m_Thresholds[i])
                {
                    m_PreviousValue = value;
                    m_NextRevokeTime = m_GetRevokeDuration != null ? Time.time + m_GetRevokeDuration() : float.MaxValue;
                    m_Callback?.Invoke(value);
                    break;
                }
            }
        }
    }
}