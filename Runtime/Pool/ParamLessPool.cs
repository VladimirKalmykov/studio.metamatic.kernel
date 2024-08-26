using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic
{
    public class ParamLessPool
    {
        private static readonly Dictionary<Type, ParamLessPool<object>> Pools = new();
        
        /// <summary>
        /// Get object from global pool. It is shortest way to get object from pool without creating instance of pool.
        /// The pool will be created if it doesn't exist. It will be automatically dissolved after prolonged downtime.
        /// Capacity of this type of pool is zero and auto-decreased to zero after prolonged downtime.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T Get<T>()
        {
            var type = typeof(T);
            if (!Pools.TryGetValue(type, out var pool))
            {
                pool = Pools[type] = new ParamLessPool<object>(autoShrink: true, capacity: 0);
                pool.OnPoolIsUseless += () =>
                {
                    /* Remove useless pool */
                    Debug.Log($"[POOL] Global pool of type {type} is useless. Removing from global pool");
                    pool.Dispose();
                    Pools.Remove(type);
                };
            }
            return (T) pool.Get();
        }
        
        public static void Return<T>(T obj)
        {
            var type = typeof(T);
            if (!Pools.TryGetValue(type, out var pool))
            {
                Debug.LogWarning("Trying to return object to non-existent pool. Object will be disposed");
                /* Dispose object if pool doesn't exist */
                if (obj is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            else
            {
                pool.Return(obj);
            }
        }
    } 
    public class ParamLessPool<T> : ParamLessPool, IDisposable
    {
        #region ShrinkTimer

        public enum ShrinkTimestampType
        {
            Analysis,
            Shrinking
        }
        private static bool _BackgroundTimeIsEnabled;
        private static int _BackgroundTrackingInquiryCount;
        private float m_NextShrinkTime;
        private Timer m_BackgroundRecheckTimer;
        private event Action OnBackgroundTrack;
        public const int DefaultShrinkingPeriod = 600;
        public const int DefaultAnalysisPeriod = 60;
        
        /// <summary>
        /// Subscribe to the event that will be called every minute in the background
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public DisposableAction SubscribeAutoShrinkTime(Action<ShrinkTimestampType> action)
        {
            _BackgroundTrackingInquiryCount++;
            if (!_BackgroundTimeIsEnabled)
            {
                EnableBackgroundTracking();
            }
            
            m_NextShrinkTime = Time.time + DefaultShrinkingPeriod;
            void Handler()
            {
                action.Invoke(ShrinkTimestampType.Analysis);
                if (Time.time >= m_NextShrinkTime)
                {
                    m_NextShrinkTime = Time.time + DefaultShrinkingPeriod;
                    action.Invoke(ShrinkTimestampType.Shrinking);
                }
            }
            OnBackgroundTrack += Handler;
            return new DisposableAction(() =>
            {
                OnBackgroundTrack -= Handler;
                _BackgroundTrackingInquiryCount--;
                if (_BackgroundTrackingInquiryCount == 0)
                {
                    DisableBackgroundTracking();
                }
            });
        }

        private void EnableBackgroundTracking()
        {
            if (!_BackgroundTimeIsEnabled)
            {
                Debug.LogError("Cannot enable auto shrink for global pool");
                return;
            }
            _BackgroundTimeIsEnabled = true;
            m_BackgroundRecheckTimer = new Timer((state) => InternalOnBackgroundTrack(state).Forget(), null, (int)(DefaultAnalysisPeriod * 1000), (int)(DefaultAnalysisPeriod * 1000));
        }
        
        private void DisableBackgroundTracking()
        {
            _BackgroundTimeIsEnabled = false;
            m_BackgroundRecheckTimer?.Dispose();
        }

        private async UniTaskVoid InternalOnBackgroundTrack(object state)
        {
            await UniTask.SwitchToMainThread();
            OnBackgroundTrack?.Invoke();
        }
        #endregion
        
        
        private readonly Queue<T> m_ObjectsInPool = new();
        private HashSet<int> m_OutOfPoolHashes = new();
        
        private int m_Capacity;
        private int m_Limit = int.MaxValue;
        private bool m_AutoShrink;
        private AverageFruitBasket m_AverageFruitBasket;
        private Action m_OnDestroyHandler;
        
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
        /// <summary>
        /// The event is called when the pool has no objects in it and there's no objects out of pool
        /// </summary>
        public event Action OnPoolIsUseless;
        

        public ParamLessPool()
        {
           
        }
        
        public ParamLessPool(int capacity = 0, int limit = Int32.MaxValue, bool autoShrink = false): this()
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
            m_AutoShrink = autoShrink;
            for (int i = 0; i < capacity; i++)
            {
                m_ObjectsInPool.Enqueue(Activator.CreateInstance<T>());
            }

            if (autoShrink)
            {
                /* Start auto shrink timer */
                m_AverageFruitBasket = new AverageFruitBasket(600);
                m_OnDestroyHandler += SubscribeAutoShrinkTime(AutoShrinkTimestamp);
            }
        }

        ~ParamLessPool()
        {
            m_OnDestroyHandler?.Invoke();
        }
        
        /// <summary>
        /// Shrink time event handler
        /// </summary>
        /// <param name="type"></param>
        private void AutoShrinkTimestamp(ShrinkTimestampType type)
        {
            switch (type)
            {
                case ShrinkTimestampType.Analysis:
                    m_AverageFruitBasket.SetCount(m_ObjectsInPool.Count, Time.time);
                    break;
                case ShrinkTimestampType.Shrinking:
                    var maxCount = m_AverageFruitBasket.GetMaxCount();
                    Debug.Log($"[AUTOSHRINK] Max count for the period is {maxCount} (Current in pool: {m_ObjectsInPool.Count}, Current out of pool: {OutOfPoolCount})");
                    var countInPool = m_ObjectsInPool.Count;
                    var countToRemove = (countInPool + OutOfPoolCount) - maxCount;
                    if (countToRemove > 0)
                    {
                        Debug.Log($"[AUTOSHRINK] Decided to remove {countToRemove} objects from pool");
                        for (var i = 0; i < countToRemove; i++)
                        {
                            if (m_ObjectsInPool.Count > 0)
                            {
                                var obj = m_ObjectsInPool.Dequeue();
                                if (obj is IDisposable disposable)
                                {
                                    disposable.Dispose();
                                }
                            }
                        }
                    }
                    if (maxCount == 0 && m_ObjectsInPool.Count == 0 && OutOfPoolCount == 0)
                    {
                        Debug.Log("[AUTOSHRINK] Pool is useless. Triggering OnPoolIsEmpty event");
                        OnPoolIsUseless?.Invoke();
                    }
                    break;
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
            if (m_ObjectsInPool.Contains(obj))
            {
                throw new Exception("Object is already in pool");
            }
            #if DEBUG
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