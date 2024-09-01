namespace Metamatic
{
    using System;
    using System.Collections.Generic;

    public class LimitedQueue<T>
    {
        private readonly Queue<T> m_Queue = new Queue<T>();
        private readonly int m_MaxSize;
        
        public Queue<T> originalQueue => m_Queue;

        public LimitedQueue(int maxSize)
        {
            if (maxSize <= 0)
            {
                throw new ArgumentException("Max size must be greater than zero", nameof(maxSize));
            }
            m_MaxSize = maxSize;
        }

        public void Enqueue(T item)
        {
            if (m_Queue.Count >= m_MaxSize)
            {
                m_Queue.Dequeue();
            }
            m_Queue.Enqueue(item);
        }

        public T Dequeue()
        {
            return m_Queue.Dequeue();
        }

        public int count => m_Queue.Count;

        public T Peek()
        {
            return m_Queue.Peek();
        }

        public void Clear()
        {
            m_Queue.Clear();
        }

        public bool Contains(T item)
        {
            return m_Queue.Contains(item);
        }

        public T[] ToArray()
        {
            return m_Queue.ToArray();
        }
    }

}