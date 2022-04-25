using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Metamatic {
    public class MeanValue<T>: IEnumerable<T>
        where T: notnull
    {
        public delegate T Summ(T a, T b);
        private T[] m_Values;
        private int m_Pointer;
        private int m_InitializedLength;
        private Summ m_Summ;
        public MeanValue(int length, Summ summ)
        {
            if (length < 1)
            {
                throw new Exception("The length can not be lower than 1");
            }

            m_Summ = summ;
            m_Values = new T[length];
        }

        public T Set(T value)
        {
            m_InitializedLength = Mathf.Max(m_InitializedLength, m_Pointer + 1);
            m_Values[m_Pointer] = value;
            m_Pointer++;
            if (m_Pointer >= m_Values.Length)
            {
                m_Pointer = 0;
            }

            var sum = m_Values[0];
            for (var i = 1; i < m_InitializedLength; i++)
            {
                sum = m_Summ(sum, m_Values[i]);
            }

            return sum;
        }
        
        public IEnumerator<T> GetEnumerator()
        {
            for (var i = 0; i < m_InitializedLength; i++)
            {
                yield return m_Values[m_Pointer];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}