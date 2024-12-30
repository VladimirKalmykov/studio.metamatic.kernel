using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

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
        
        /// <summary>
        /// Sync Lock List for a period of time. 
        /// </summary>
        /// <returns></returns>
        public IDisposable Lock()
        {
            return m_Lock.Lock();
        }
        
        /// <summary>
        /// Await for the lock to be released and then lock the list again.
        /// </summary>
        /// <param name="signal"></param>
        /// <returns></returns>
        public UniTask<DisposableAction> Lock(Signal signal)
        {
            return m_Lock.Lock(signal);
        }
        
        ~LockedList()
        {
            m_Lock.Dispose();
        }
    }
}