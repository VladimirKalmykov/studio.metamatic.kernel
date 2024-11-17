namespace Metamatic
{
    using System.Collections.Generic;

    /// <summary>
    /// Measures the average value of the numbers in the queue according to the capacity.
    /// For example if the capacity is 5, then the average value will be calculated for the last 5 numbers.
    /// If number of numbers in the queue is less than the capacity, then the average value will be calculated for sum of all numbers in the queue divided by the capacity (e.g. [1] => 1/5 = 0.2)
    /// </summary>
    public class Average
    {
        private readonly int m_Capacity;
        private readonly Queue<float> m_Numbers;
        private float m_Sum;

        public Average(int capacity)
        {
            this.m_Capacity = capacity;
            this.m_Numbers = new Queue<float>(capacity);
            this.m_Sum = 0f;
        }

        public float Set(float value)
        {
            m_Numbers.Enqueue(value);
            m_Sum += value;

            if (m_Numbers.Count > m_Capacity)
            {
                float removedValue = m_Numbers.Dequeue();
                m_Sum -= removedValue;
            }

            return m_Sum / m_Capacity;
        }
    
        public void Reset()
        {
            m_Numbers.Clear();
            m_Sum = 0f;
        }

        public float result
        {
            get
            {
                if (m_Numbers.Count == 0)
                    return 0f;

                return m_Sum / m_Capacity;
            }
        }
    }
}