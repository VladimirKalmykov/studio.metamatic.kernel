using System;
using System.Collections.Generic;

namespace Metamatic
{
    /// <summary>
    /// Variant of List that can be async locked for a period of time.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class LockedList<T>: List<T>
    {
        private readonly DeferLock m_Lock = new ();
        public bool isLocked => m_Lock.IsLocked;
        public DeferLock GetAwaiter() => m_Lock;
        
        public IDisposable Lock()
        {
            return m_Lock.Lock();
        }
        
        ~LockedList()
        {
            m_Lock.Dispose();
        }
    }
}