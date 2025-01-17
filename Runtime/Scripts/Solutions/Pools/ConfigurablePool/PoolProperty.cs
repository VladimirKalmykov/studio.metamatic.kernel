using System;

namespace Metamatic
{
    public readonly struct PoolProperty<T>: IDisposable
    {
        private readonly T m_Property;
        private readonly Action<T> m_Return;
        private readonly Func<T, bool> m_IsInPool;
        
        public T property => m_IsInPool(m_Property) ? default : m_Property;
        
        public PoolProperty(T property, Action<T> returnAction, Func<T, bool> isInPool)
        {
            m_Property = property;
            m_Return = returnAction;
            m_IsInPool = isInPool;
        }


        public void Dispose()
        {
            if (!m_IsInPool(m_Property))
            {
                m_Return(m_Property);
            }
        }
    }
}