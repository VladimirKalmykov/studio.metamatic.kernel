using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic
{
    public class ParamLessPool<T> : IDisposable
    {
        private readonly Stack<T> m_ObjectsInPool = new();
        #if DEBUG
        private readonly List<T> m_ObjectOutOfPool = new();
        private PoolMetrics m_PoolMetrics;
        #endif

        public ParamLessPool()
        {
            #if DEBUG
            m_PoolMetrics = new PoolMetrics(30000);
            m_PoolMetrics.onResult = (stats) => Debug.Log($"[POOL_METRICS] Recommended capacity for pool is {stats.recommendedCapacity}, Recommended limit is {stats.recommendedLimit}");
            #endif
        }
        
        public ParamLessPool(int capacity = 0): this()
        {
            for (int i = 0; i < capacity; i++)
            {
                m_ObjectsInPool.Push(Activator.CreateInstance<T>());
            }
        }
        
        public T Get()
        {
            if (m_ObjectsInPool.Count > 0)
            {
                var outOfPool = m_ObjectsInPool.Pop();
                #if DEBUG
                m_ObjectOutOfPool.Add(outOfPool);
                #endif
                return outOfPool;
            }
            else
            {
                var outOfPool = Activator.CreateInstance<T>();
                #if DEBUG
                m_ObjectOutOfPool.Add(outOfPool);
                #endif
                return outOfPool;
            }
        }

        public void Release(T obj)
        {
            #if DEBUG
            /* Check that released object is not in pool */
            if (m_ObjectOutOfPool.Contains(obj))
            {
                throw new Exception("Object is already released");
            }
            /* Check that release object is contained in out of pool list */
            if (!m_ObjectOutOfPool.Contains(obj))
            {
                throw new Exception("Object is not belong to this pool");
            }
            #endif
            m_ObjectsInPool.Push(obj);
            
        }

        public void TryRelease(T obj)
        {
            if (m_ObjectsInPool.Contains(obj)) return;
            Release(obj);
        }

        public void Dispose()
        {
            #if DEBUG
            if (m_ObjectOutOfPool.Count > 0)
            {
                throw new Exception("You are trying to dispose pool with objects that are not released");
            }
            #endif
            /* Clear pool */
            m_ObjectsInPool.Clear();
            #if DEBUG
            m_ObjectOutOfPool.Clear();
            #endif
        }
    }
}