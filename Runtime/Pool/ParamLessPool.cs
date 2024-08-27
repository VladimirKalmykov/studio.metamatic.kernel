using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic
{
    public class ParamLessPool
    {
    }

    public class ParamLessPool<T> : ParamLessPool, IDisposable
    {
        #region GlobalPool

        private static ParamLessPool<T> _SharedPool;
        /// <summary>
        /// Get object from global pool. It is shortest way to get object from pool without creating instance of pool.
        /// The pool will be created if it doesn't exist. It will be automatically dissolved after prolonged downtime.
        /// Capacity of this type of pool is zero and auto-decreased to zero after prolonged downtime.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T GetShared()
        {
            var pool = GetSharedPool();
            return pool.Get();
        }
        
        private static ParamLessPool<T> GetSharedPool()
        {
            if (_SharedPool == null)
            {
                var pool = _SharedPool = new ParamLessPool<T>(autoShrink: true, capacity: 0);
                pool.OnPoolIsUseless += () =>
                {
                    /* Remove useless pool */
                    Debug.Log($"[POOL] Global pool of type {typeof(T)} is useless. Removing from global pool");
                    _SharedPool.Dispose();
                    _SharedPool = null;
                };
            }

            return _SharedPool;
        }

        public static void ReturnShared(T obj)
        {
            if (_SharedPool == null)
            {
                Debug.LogError("Trying to return object to non-existent global pool. Object will be disposed");
                if (obj is IDisposable disposable)
                {
                    disposable.Dispose();
                }
                return;
            }

            _SharedPool.Return(obj);
        }

        /// <summary>
        /// Create in shared pool specified count of objects and prevent them from auto-shrinking for the next period (see DefaultShrinkingPeriod)
        /// </summary>
        /// <param name="count"></param>
        /// <param name="lazy">Create just one object per frame</param>
        public static void PreWarmShared(int count, bool lazy = false)
        {
            var pool = GetSharedPool();
            
            /* Set count of objects in pool to the fruit basket to keep this count for next AutoShrink period */
            pool.m_FruitBasket?.SetCount(count, Time.time);

            if (Application.isPlaying && lazy)
            {
                LazyPreWarmPool(pool, count).Forget();
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    pool.Return(pool.Get(true));
                }
            }
        }
        
        public static async UniTaskVoid LazyPreWarmPool(ParamLessPool<T> pool, int count)
        {
            var preWarmedItems = ParamLessPool<List<T>>.GetShared();
            for (int i = 0; i < count; i++)
            {
                preWarmedItems.Add(pool.Get(true));
                await UniTask.Yield();
            }

            foreach (var item in preWarmedItems)
            {
                pool.Return(item);
            }
            preWarmedItems.Clear();
            ParamLessPool<List<T>>.ReturnShared(preWarmedItems);
        }

#if DEBUG
        public static ParamLessPool<T> DebugGetSharedPool()
        {
            return _SharedPool;
        }
#endif

        #endregion

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

#if DEBUG
        // ReSharper disable once FieldCanBeMadeReadOnly.Global
        // ReSharper disable once StaticMemberInGenericType
        // ReSharper disable once MemberCanBePrivate.Global
        public static float DefaultShrinkingPeriod = 20f;

        // ReSharper disable once FieldCanBeMadeReadOnly.Global
        // ReSharper disable once StaticMemberInGenericType
        // ReSharper disable once MemberCanBePrivate.Global
        public static float DefaultAnalysisPeriod = 5f;
#else
        public const float DefaultShrinkingPeriod = 600;
        public const float DefaultAnalysisPeriod = 60;
#endif

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
                Debug.Log($"[AUTO-SHRINK] Enabling background tracking.");
                EnableBackgroundTracking();
            }
            Debug.Log($"[AUTO-SHRINK] Subscribed to auto shrink event. Current subscribers: {_BackgroundTrackingInquiryCount}");

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
            if (_BackgroundTimeIsEnabled)
            {
                Debug.LogError("Cannot enable auto shrink for global pool twice");
                return;
            }

            _BackgroundTimeIsEnabled = true;
            m_BackgroundRecheckTimer = new Timer((state) => InternalOnBackgroundTrack(state).Forget(), null,
                (int)(DefaultAnalysisPeriod * 1000), (int)(DefaultAnalysisPeriod * 1000));
        }

        private void DisableBackgroundTracking()
        {
            Debug.Log($"[AUTO-SHRINK] Disabling background tracking.");
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
        private AverageFruitBasket m_FruitBasket;
        private Action m_OnDisposeHandler;

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

        public ParamLessPool(int capacity = 0, int limit = Int32.MaxValue, bool autoShrink = false) : this()
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
                m_FruitBasket = new AverageFruitBasket(DefaultShrinkingPeriod);
                m_OnDisposeHandler += SubscribeAutoShrinkTime(AutoShrinkTimestamp);
            }
        }

        ~ParamLessPool()
        {
            Dispose();
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
                    m_FruitBasket.SetCount(OutOfPoolCount, Time.time);
                    Debug.Log($"[AUTOSHRINK] Analysis event. Current in pool: {m_ObjectsInPool.Count}, Current out of pool: {OutOfPoolCount}, Max count: {m_FruitBasket.GetMaxCount()}");
                    break;
                case ShrinkTimestampType.Shrinking:
                    var maxCount = m_FruitBasket.GetMaxCount();
                    Debug.Log(
                        $"[AUTOSHRINK] Shrinking is active. Max count for the period is {maxCount} (Current in pool: {m_ObjectsInPool.Count}, Current out of pool: {OutOfPoolCount})");
                    var countInPool = m_ObjectsInPool.Count;
                    var countToRemove = (countInPool + OutOfPoolCount) - maxCount;
                    if (countToRemove > 0 && countToRemove >= countInPool)
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
        /// Get object from pool. If pool is empty or forceCreate is true, new object will be created
        /// </summary>
        /// <returns></returns>
        public T Get(bool forceCreate = false)
        {
            var outOfPool = m_ObjectsInPool.Count == 0 || forceCreate ? Activator.CreateInstance<T>() : m_ObjectsInPool.Dequeue();
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
            }
            else if (m_ObjectsInPool.Contains(obj))
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
            m_OnDisposeHandler?.Invoke();
            /* Clear pool */
            m_ObjectsInPool.Clear();
            m_OutOfPoolHashes.Clear();
            GC.SuppressFinalize(this);
        }
    }
}