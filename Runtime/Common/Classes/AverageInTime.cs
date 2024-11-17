using System.Collections.Generic;
using UnityEngine;

namespace Metamatic
{
    /// <summary>
    /// Measures the average value over a specified time period.
    /// If the measurement period hasn't fully elapsed, the result is proportionally less.
    /// </summary>
    public class AverageInTime
    {
        private readonly float m_MeasurementPeriod;
        private readonly Queue<DataPoint> m_DataPoints;
        private float m_Sum;

        // Вспомогательный класс для хранения значения и времени
        private class DataPoint
        {
            public float value { get; }
            public float time { get; }

            public DataPoint(float value, float time)
            {
                this.value = value;
                this.time = time;
            }
        }

        public AverageInTime(float periodInSeconds)
        {
            m_MeasurementPeriod = periodInSeconds;
            m_DataPoints = new Queue<DataPoint>();
            m_Sum = 0f;
        }

        /// <summary>
        /// Adds a new value and returns the current average.
        /// </summary>
        /// <param name="value">The new value to add.</param>
        /// <returns>The current average.</returns>
        public float Set(float value)
        {
            float currentTime = Time.time;

            // Добавляем новую точку данных
            DataPoint newDataPoint = new DataPoint(value, currentTime);
            m_DataPoints.Enqueue(newDataPoint);
            m_Sum += value;

            // Удаляем устаревшие точки данных
            float cutoffTime = currentTime - m_MeasurementPeriod;
            while (m_DataPoints.Count > 0 && m_DataPoints.Peek().time < cutoffTime)
            {
                DataPoint oldDataPoint = m_DataPoints.Dequeue();
                m_Sum -= oldDataPoint.value;
            }

            // Вычисляем среднее значение
            float elapsedTime = currentTime - m_DataPoints.Peek().time;
            float scalingFactor = Mathf.Min(elapsedTime / m_MeasurementPeriod, 1f);
            float average = (m_Sum / m_DataPoints.Count) * scalingFactor;

            return average;
        }

        /// <summary>
        /// Gets the current average without adding a new value.
        /// </summary>
        public float Result
        {
            get
            {
                float currentTime = Time.time;

                // Удаляем устаревшие точки данных
                float cutoffTime = currentTime - m_MeasurementPeriod;
                while (m_DataPoints.Count > 0 && m_DataPoints.Peek().time < cutoffTime)
                {
                    DataPoint oldDataPoint = m_DataPoints.Dequeue();
                    m_Sum -= oldDataPoint.value;
                }

                if (m_DataPoints.Count == 0)
                    return 0f;

                // Вычисляем среднее значение
                float elapsedTime = currentTime - m_DataPoints.Peek().time;
                float scalingFactor = Mathf.Min(elapsedTime / m_MeasurementPeriod, 1f);
                float average = (m_Sum / m_DataPoints.Count) * scalingFactor;

                return average;
            }
        }

        /// <summary>
        /// Resets the average calculation.
        /// </summary>
        public void Reset()
        {
            m_DataPoints.Clear();
            m_Sum = 0f;
        }
    }
}
