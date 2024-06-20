using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Metamatic {
    /// <summary>
    /// Collect T values and returns more saturated of them in a time.
    /// </summary>
    /// <performance>.Net tests show 1,000,000 iterations per 136ms</performance>
    /// <typeparam name="T"></typeparam>
    public class SaturateValue<T>
        where T : IConvertible
    {
        private T[] m_Values;
        private float[] m_Weights;
        private float m_MaxTime;
        private float m_CurrentValueTime;
        private int m_CurrentValueIndex = -1;
        private int m_InitializedCount;
        private Func<T, T, bool> m_Compare;
        private float m_Sum;
        private Dictionary<T, int> m_TIndexMap;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="maxTime"></param>
        /// <param name="maxCount"></param>
        /// <param name="compare">Specifiing comparing function increases 15% performance!</param>
        public SaturateValue(float maxTime = 1f, int maxCount = 10, Func<T, T, bool> compare = null)
        {
            m_TIndexMap = new Dictionary<T, int>();
            m_Compare = compare == null ? IsEqual : compare;
            if (maxCount < 2)
            {
                throw new Exception("The length can not be lower than 2");
            }

            m_Values = new T[maxCount];
            m_Weights = new float[maxCount];
            m_MaxTime = maxTime;
        }

        private static float GetCurrentTime()
        {
            return Time.time;
        }

        public T Set(T value, bool immediate = false)
        {
            var cachedCurrentTime = GetCurrentTime();
            if (immediate)
            {
                for (var i = 0; i < m_InitializedCount; i++)
                {
                    m_Weights[i] = 0f;
                }

                if (!m_TIndexMap.TryGetValue(value, out m_CurrentValueIndex))
                {
                    AddItem(value);
                }

                m_Weights[m_CurrentValueIndex] = 1f;
                m_CurrentValueTime = cachedCurrentTime;
                m_Sum = 1f;
                return value;
            }
            
            if (m_InitializedCount == 0)
            {
                AddItem(value);
                m_CurrentValueTime = cachedCurrentTime;
                return value;
            }
            var cachedCurrentWeight = m_Weights[m_CurrentValueIndex];
            /* Previous value weight */
            var diff = cachedCurrentTime - m_CurrentValueTime;
            var weight = Math.Min((float)(diff) / (float)m_MaxTime, 1f);

            /* Get new weight for the existen value */
            var newWeight = Math.Min(1f, cachedCurrentWeight + weight);

            /* Get ratios of rest values */
            var sum = m_Sum - cachedCurrentWeight;

            /* Get possible rest weight */
            var restWeight = 1f - newWeight;

            /* Apply weight to the rest values */
            var restMax = newWeight;
            var valueWithMaxWeightIndex = m_CurrentValueIndex;
            m_Sum = newWeight;
            if (sum > 0)
            {
                for (var i = 0; i < m_InitializedCount; i++)
                {
                    var itemWeight = m_Weights[i];
                    var next = Math.Min(itemWeight, (itemWeight / sum) * restWeight);
                    if (next > restMax)
                    {
                        valueWithMaxWeightIndex = i;
                        restMax = next;
                    }
                    m_Sum += next;
                    m_Weights[i] = next;
                }
                m_Sum -= (cachedCurrentWeight / sum) * restWeight;
            }
            else
            {
                for (var i = 0; i < m_InitializedCount; i++)
                {
                    m_Weights[i] = 0f;
                }
            }

            m_Weights[m_CurrentValueIndex] = newWeight;

            /* Find next value index */
            // Array.FindIndex(m_Values, x => m_Compare(x, value));
            if (!m_TIndexMap.TryGetValue(value, out m_CurrentValueIndex))
            {
                AddItem(value);
            }
            m_CurrentValueTime = cachedCurrentTime;

            return m_Values[valueWithMaxWeightIndex];
        }

        private static bool IsEqual(T x, T y)
        {
            return typeof(T).IsValueType
                ? object.Equals(x, y)
                : ReferenceEquals(x, y); ;
        }

        private void AddItem(T value)
        {
            if (m_InitializedCount >= m_Values.Length)
            {
                var minWeight = m_Weights.Min(x => x);
                m_CurrentValueIndex = Array.FindIndex(m_Weights, x => x == minWeight);
            } else
            {
                m_InitializedCount++;
                m_CurrentValueIndex = m_InitializedCount - 1;
            }
            m_Values[m_CurrentValueIndex] = value;
            m_Weights[m_CurrentValueIndex] = 0f;
            m_TIndexMap[value] = m_CurrentValueIndex;
        }
    }
}