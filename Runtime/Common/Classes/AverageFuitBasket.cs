namespace Metamatic
{
    using System;
    using System.Collections.Generic;

    public class AverageFruitBasket
    {
        private readonly float m_BurningTime;
        private readonly Queue<(int count, float time)> m_Records = new Queue<(int count, float time)>();
        private int m_TotalFruits = 0;

        public AverageFruitBasket(float burningTime)
        {
            this.m_BurningTime = burningTime;
        }

        public void SetCount(int count, float time)
        {
            // Удаляем устаревшие записи
            while (m_Records.Count > 0 && time - m_Records.Peek().time > m_BurningTime)
            {
                var oldestRecord = m_Records.Dequeue();
                m_TotalFruits -= oldestRecord.count;
            }

            // Добавляем новую запись
            m_Records.Enqueue((count, time));
            m_TotalFruits += count;
        }

        /// <summary>
        /// Get the average count of fruits in the basket for the last burningTime minutes
        /// </summary>
        /// <returns></returns>
        public float GetAverageCount()
        {
            if (m_Records.Count == 0)
            {
                return 0;
            }

            // Возвращаем среднее значение за последние burningTime минут
            return m_TotalFruits / (float)m_Records.Count;
        }

        /// <summary>
        /// Get the maximum count of fruits in the basket for the last burningTime minutes
        /// </summary>
        /// <returns></returns>
        public int GetMaxCount()
        {
            if (m_Records.Count == 0)
            {
                return 0;
            }

            // Возвращаем максимальное значение за последние burningTime минут
            int maxCount = 0;
            foreach (var record in m_Records)
            {
                maxCount = Math.Max(maxCount, record.count);
            }

            return maxCount;
        }
    }
}