using System;
using System.Collections.Generic;
using UnityEngine;

namespace Metamatic
{
    public class ParamLessPool<T> : IDisposable
    {
        private readonly Queue<T> m_ObjectsInPool = new();
        private HashSet<int> m_OutOfPoolHashes = new();
        
        private int m_Capacity;
        private int m_Limit = int.MaxValue;
        
        /// <summary>
        /// Count of objects in pool (available for get)
        /// </summary>
        // ReSharper disable once InconsistentNaming
        public int Count => m_ObjectsInPool.Count;
        /// <summary>
        /// Count of objects out of pool (not available for get, but still belongs to pool)
        /// </summary>
        // ReSharper disable once InconsistentNaming
        public int OutOfPoolCount => m_OutOfPoolHashes.Count;

        public ParamLessPool()
        {
           
        }
        
        public ParamLessPool(int capacity = 0, int limit = Int32.MaxValue): this()
        {
            #if DEBUG
            /* Detect that the object is a class and it is derived from UnityEngine.Object */
            if (typeof(T).IsClass && typeof(T).IsSubclassOf(typeof(UnityEngine.Object)))
            {
                throw new Exception("Do not use Unity objects in ParamLessPool. Use UnityObjectPool instead.");
            }
            #endif
            m_Capacity = capacity;
            m_Limit = limit;
            for (int i = 0; i < capacity; i++)
            {
                m_ObjectsInPool.Enqueue(Activator.CreateInstance<T>());
            }
        }
        
        /// <summary>
        /// Get object from pool. If pool is empty, new object will be created
        /// </summary>
        /// <returns></returns>
        public T Get()
        {
            var outOfPool = m_ObjectsInPool.Count == 0 ? Activator.CreateInstance<T>() : m_ObjectsInPool.Dequeue();
            m_OutOfPoolHashes.Add(outOfPool.GetHashCode());
            return outOfPool;
        }
        
        /// <summary>
        /// Return object back to pool
        /// </summary>
        /// <param name="obj"></param>
        /// <exception cref="Exception"></exception>
        public void Return(T obj)
        {
            #if DEBUG
            /* Check that returned object is not in pool */
            if (m_ObjectsInPool.Contains(obj))
            {
                throw new Exception("Object is already in pool");
            }
            /* Check that returned object is contained in out of pool list */
            if (!m_OutOfPoolHashes.Contains(obj.GetHashCode()))
            {
                throw new Exception("Object is not belong to this pool");
            }
            #endif
            m_OutOfPoolHashes.Remove(obj.GetHashCode());
            if (m_ObjectsInPool.Count < m_Limit)
            {
                m_ObjectsInPool.Enqueue(obj);
            }
            else
            {
                #if DEBUG
                Debug.LogWarning("Pool limit reached. Object will be destroyed");
                #endif
                if (obj is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }
        
        /// <summary>
        /// Determines whether object belongs to pool (was created by pool and wasn't released or detached)
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public bool BelongsToPool(T obj)
        {
            return m_OutOfPoolHashes.Contains(obj.GetHashCode());
        }
        
        /// <summary>
        /// Release object. Released object will not return to the pool. It will be removed from the pool memory that it object belongs to pool.
        /// </summary>
        /// <param name="obj"></param>
        /// <exception cref="Exception"></exception>
        public void Release(T obj)
        {
#if DEBUG
            /* Check released object is not present in pool */
            if (m_ObjectsInPool.Contains(obj))
            {
                throw new Exception("Object is in pool, you can not release. Get it from pool first.");
            }
#endif
            /* Check that released object is exists in pool or out of pool lists */
            if (!m_OutOfPoolHashes.Contains(obj.GetHashCode()))
            {
                throw new Exception("Object is not belong to this pool");
            }

            var hashCode = obj.GetHashCode();
            if (m_OutOfPoolHashes.Contains(hashCode))
            {
                m_OutOfPoolHashes.Remove(hashCode);
            }
        }
        
        /// <summary>
        /// Heavy operation! If you need detach from the pool concrete object even if its in the pool right now, use this method.
        /// </summary>
        /// <param name="obj"></param>
        public void HardDetach(T obj)
        {
            var hashCode = obj.GetHashCode();
            if (m_OutOfPoolHashes.Contains(hashCode))
            {
                m_OutOfPoolHashes.Remove(hashCode);
            } else if (m_ObjectsInPool.Contains(obj))
            {
                /* Create copy of queue */
                var queueCopy = new Queue<T>(m_ObjectsInPool);
                m_ObjectsInPool.Clear();
                foreach (var item in queueCopy)
                {
                    if (!item.Equals(obj))
                    {
                        m_ObjectsInPool.Enqueue(item);
                    }
                }
            }
        }

        public void Dispose()
        {
            /* Clear pool */
            m_ObjectsInPool.Clear();
            m_OutOfPoolHashes.Clear();
        }
    }
}