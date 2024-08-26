namespace Metamatic
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Cysharp.Threading.Tasks;

    namespace Metamatic
    {
        /// <summary>
        /// Variant of List that can be async locked for a period of time.
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        public class LockedDictionary<TKey, TValue>: Dictionary<TKey, TValue>, IDisposable
        {
            private readonly DeferLock m_Lock = new();
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
            /// <param name="cancellationToken"></param>
            /// <returns></returns>
            public UniTask<DisposableAction> Lock(CancellationToken cancellationToken)
            {
                return m_Lock.Lock(cancellationToken);
            }
        
            ~LockedDictionary()
            {
                Dispose();
            }

            public void Dispose()
            {
                m_Lock.Dispose();
                GC.SuppressFinalize(this);
            }
        }
    }
}