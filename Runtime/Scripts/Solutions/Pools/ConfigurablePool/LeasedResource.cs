using System;

namespace Metamatic
{
    /// <summary>
    /// Leased resource is a resource that is leased from the pool (or some other sync storage) and should be returned to the pool after usage.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public readonly struct LeasedResource<T>: IDisposable
    {
        private static readonly Action<T> DefaultReturn = _ => { };
        public static readonly LeasedResource<T> Empty = new LeasedResource<T>(default, DefaultReturn, _ => false); 
        private readonly T m_Property;
        private readonly Action<T> m_Return;
        private readonly Func<T, bool> m_IsInPool;
        public bool isEmpty => m_Return == DefaultReturn;
        
        public T resource => m_IsInPool(m_Property) ? default : m_Property;
        
        public LeasedResource(T property, Action<T> returnAction, Func<T, bool> isInPool)
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