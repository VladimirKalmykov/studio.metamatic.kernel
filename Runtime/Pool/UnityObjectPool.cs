using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic
{
    /// <summary>
    /// Pool for unity objects
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class UnityObjectPool<T>: Pool, IUnityObjectPool<T>
    where T: UnityEngine.Object
    {
        protected readonly Stack<T> objectsInPool;
        protected readonly LinkedList<T> allProducedObjects = new ();
        private readonly Func<T> m_OnCreate;
        private readonly Action<T> m_OnGet;
        private readonly Action<T> m_OnRelease;
        protected readonly Action<T> onDestroy;
        private int m_DefaultCapacity;
        protected int capacity { get; private set; }
        private Dictionary<string, int> m_CapacityOverrides = new ();
        
        public UnityObjectPool(Func<T> onCreate, Action<T> onGet = null, Action<T> onRelease = null, Action<T> onDestroy = null, int capacity = 1)
        {
            objectsInPool = new Stack<T>(capacity);
            m_OnCreate = onCreate;
            m_OnGet = onGet;
            m_OnRelease = onRelease;
            this.onDestroy = onDestroy ?? DefaultDestroy;
            this.m_DefaultCapacity = capacity;
            #if DEBUG
            debugUniqueId = Guid.NewGuid().ToString();
            #endif
        }
        
        /// <summary>
        /// Method allows temporarily override the capacity of the pool. New value associated with the specific unique id and can be released by calling ReleaseTemporaryConfig.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="overridingCapacity"></param>
        public void SetTemporaryCapacity(string id, int overridingCapacity)
        {
            m_CapacityOverrides[id] = overridingCapacity;
            UpdateCapacity(m_CapacityOverrides.Values.Sum() + m_DefaultCapacity);
        }
        
        /// <summary>
        /// Release all configurations associated with the specific unique id. This method is used in pull reservation and provide ability to release temporary configurations created by SetCapacity.
        /// </summary>
        /// <param name="id"></param>
        protected void ReleaseTemporaryConfig(string id)
        {
            if (m_CapacityOverrides.ContainsKey(id))
            {
                m_CapacityOverrides.Remove(id);
            }

            UpdateCapacity(m_CapacityOverrides.Values.Sum() + m_DefaultCapacity);
        }
        
        private void UpdateCapacity(int newCapacity)
        {
            this.capacity = newCapacity;
            /* Create new objects to fill the pool with new capacity */
            while (objectsInPool.Count < newCapacity)
            {
                T obj = m_OnCreate();
                allProducedObjects.AddLast(obj);
                objectsInPool.Push(obj);
            }
        }
        
        public virtual T Get()
        {
            #if DEBUG
            if (m_DisposeCancellationTokenSource.IsCancellationRequested) throw new ObjectDisposedException(nameof(UnityObjectPool<T>));
            #endif
            outOfPoolObjectsCount++;
            if (objectsInPool.Count > 0)
            {
                T obj = objectsInPool.Pop();
                m_OnGet?.Invoke(obj);
                return obj;
            }
            else
            {
                T obj = m_OnCreate();
                allProducedObjects.AddLast(obj);
                m_OnGet?.Invoke(obj);
                return obj;
            }
        }

        public void SetCapacity(int capacity)
        {
            this.m_DefaultCapacity = capacity;
        }

        private List<T> m_ReleaseHistory = new ();
        public virtual void Return(T obj)
        {
#if DEBUG
            if (m_DisposeCancellationTokenSource.IsCancellationRequested) throw new ObjectDisposedException(nameof(UnityObjectPool<T>));
#endif
            m_ReleaseHistory.Add(obj);
            outOfPoolObjectsCount--;
            #if DEBUG
            if (outOfPoolObjectsCount < 0)
            {
                throw new InvalidOperationException("You are released to the pool more objects than you have taken!");
            }
            #endif
            m_OnRelease?.Invoke(obj);
            objectsInPool.Push(obj);
            if (outOfPoolObjectsCount == 0)
            {
                AllObjectsReleased();
            }
        }
        
        public void TryRelease(T obj)
        {
            if (objectsInPool.Contains(obj)) return;
            Return(obj);
        }
        
        private void DefaultDestroy(T obj)
        {
            if (obj != null)
            {
                UnityEngine.Object.Destroy(obj);
            }
        }
        
        public override void Dispose()
        {
            base.Dispose();
            /* Destroy all objects produced by pool */
            foreach (var obj in allProducedObjects.ToArray())
            {
                Destroy(obj);
                
            }
            objectsInPool.Clear();
            allProducedObjects.Clear();
        }
        
        protected void Destroy(T obj)
        {
            if (obj != null)
            {
                allProducedObjects.Remove(obj);
                onDestroy?.Invoke(obj);
            }
        }
    }
}