using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Metamatic.DevTools;
using UnityEngine;

namespace Metamatic
{
    public abstract class ConfigurablePool
    {
#if DEBUG
        public static int PoolsCount;
        public static int SharedPoolsCount;

        static ConfigurablePool()
        {
            Monitoring.Create("ConfigurablePool", "Total count", () => PoolsCount, PlayerLoopTiming.FixedUpdate);
            Monitoring.Create("ConfigurablePool", "Shared count", () => SharedPoolsCount, PlayerLoopTiming.FixedUpdate);
        }
#endif
    }

    public class ConfigurablePool<T> : ConfigurablePool, IDisposable
    {
        #region GlobalPool

        private static readonly Dictionary<object, ConfigurablePool<T>> _SharedPools = new();
        private static readonly Dictionary<object, ConfigurablePool<T>> _InstanceToPool = new();

        /// <summary>
        /// Get object from global pool. The pool will be created if it doesn't exist. Do not use shared pool for objects loaded from resources and addressable.
        /// </summary>
        public static T GetShared(IPoolConfig<T> config, bool forceCreate = false)
        {
            var pool = GetSharedPool(config);
            var instance = pool.Get(forceCreate);
            _InstanceToPool[instance] = pool;
            return instance;
        }

        private static ConfigurablePool<T> GetSharedPool(IPoolConfig<T> config)
        {
            var key = config.GetKey();
            if (!_SharedPools.TryGetValue(key, out var pool))
            {
                pool = new ConfigurablePool<T>(config, autoShrink: true, capacity: 0);
                _SharedPools[key] = pool;
                ConfigurablePool.SharedPoolsCount++;
                pool.OnPoolIsUseless += () =>
                {
                    ConfigurablePool.SharedPoolsCount--;
                    /* Remove useless pool */
                    Debug.Log($"[POOL] Global pool of rules {config} is useless. Removing from global pool");
                    pool.Dispose();
                    _SharedPools.Remove(key);
                };
            }
            return pool;
        }

        public static void ReturnShared(T obj)
        {
            if (_InstanceToPool.TryGetValue(obj, out var pool))
            {
                _InstanceToPool.Remove(obj);
                pool.Return(obj);
            }
            else
            {
                Debug.LogError( "[ConfigurablePool] Trying to return object to non-existent global pool. Object will be destroyed");
            }
        }

#if DEBUG
        public static int DebugGetCountInSharedPool(IPoolConfig<T> config)
        {
            var key = config.GetKey();
            if (_SharedPools.TryGetValue(key, out var pool))
            {
                return pool.Count;
            }
            return 0;
        }

        public static int DebugGetCountOutOfSharedPool(IPoolConfig<T> config)
        {
            var key = config.GetKey();
            if (_SharedPools.TryGetValue(key, out var pool))
            {
                return pool.OutOfPoolCount;
            }
            return 0;
        }

        public static ConfigurablePool<T> DebugGetSharedPool(IPoolConfig<T> config)
        {
            var key = config.GetKey();
            if (_SharedPools.TryGetValue(key, out var pool))
            {
                return pool;
            }
            return null;
        }
#endif

        /// <summary>
        /// Create in shared pool specified count of objects and prevent them from auto-shrinking for the next period (see DefaultShrinkingPeriod)
        /// </summary>
        public static void PreWarmShared(IPoolConfig<T> config, int count, bool lazy = false)
        {
            var pool = GetSharedPool(config);

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

        public static async UniTaskVoid LazyPreWarmPool(ConfigurablePool<T> pool, int count)
        {
            var preWarmedItems = new List<T>();
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
        }

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
        public static float DefaultShrinkingPeriod = 20f;
        public static float DefaultAnalysisPeriod = 5f;
#else
        public const float DefaultShrinkingPeriod = 600;
        public const float DefaultAnalysisPeriod = 60;
#endif

        /// <summary>
        /// Subscribe to the event that will be called every minute in the background
        /// </summary>
        public DisposableAction SubscribeAutoShrinkTime(Action<ShrinkTimestampType> action)
        {
            _BackgroundTrackingInquiryCount++;
            if (!_BackgroundTimeIsEnabled)
            {
#if METAMATIC_VERBOSE_LOG
                Console.Verbose("ConfigurablePool", "AutoShrink", $"Enabling background tracking.");
#endif
                EnableBackgroundTracking();
            }
#if METAMATIC_VERBOSE_LOG
            Console.Verbose("ConfigurablePool", "AutoShrink", $"Subscribed to auto shrink event. Current subscribers: {_BackgroundTrackingInquiryCount}");
#endif

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
                Console.Error("ConfigurablePool", "AutoShrink", "Cannot enable auto shrink for global pool twice");
                return;
            }

            _BackgroundTimeIsEnabled = true;
            m_BackgroundRecheckTimer = new Timer((state) => InternalOnBackgroundTrack(state).Forget(), null,
                (int)(DefaultAnalysisPeriod * 1000), (int)(DefaultAnalysisPeriod * 1000));
        }

        private void DisableBackgroundTracking()
        {
#if METAMATIC_VERBOSE_LOG
            Console.Verbose("ConfigurablePool", "AutoShrink", $"Disabling background tracking.");
#endif
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
        private readonly HashSet<T> m_OutOfPool = new();

        private readonly IPoolConfig<T> m_Config;
        private int m_Capacity;
        private int m_Limit = int.MaxValue;
        private bool m_AutoShrink;
        private AverageFruitBasket m_FruitBasket;
        private Action m_OnDisposeHandler;

        /// <summary>
        /// Count of objects in pool (available for get)
        /// </summary>
        public int Count => m_ObjectsInPool.Count;

        /// <summary>
        /// Count of objects out of pool (not available for get, but still belongs to pool)
        /// </summary>
        public int OutOfPoolCount => m_OutOfPool.Count;

#if DEBUG
        public int[] DebugGetOutOfPoolHashes()
        {
            var hashes = new int[m_OutOfPool.Count];
            var i = 0;
            foreach (var obj in m_OutOfPool)
            {
                hashes[i] = obj.GetHashCode();
                i++;
            }
            return hashes;
        }
#endif

        /// <summary>
        /// The event is called when the pool has no objects in it and there's no objects out of pool
        /// </summary>
        public event Action OnPoolIsUseless;

        public ConfigurablePool(IPoolConfig<T> config, int capacity = 0, int limit = Int32.MaxValue, bool autoShrink = false)
        {
            m_Config = config;
            ConfigurablePool.PoolsCount++;
            m_OnDisposeHandler += () =>
            {
                ConfigurablePool.PoolsCount--;
            };

#if DEBUG
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config), "Rules cannot be null.");
            }
#endif

            m_Capacity = capacity;
            m_Limit = limit;
            m_AutoShrink = autoShrink;

            for (int i = 0; i < capacity; i++)
            {
                var instance = CreateInstance();
                m_ObjectsInPool.Enqueue(instance);
                m_Config.OnReturn(instance);
            }

            if (autoShrink)
            {
                /* Start auto shrink timer */
                m_FruitBasket = new AverageFruitBasket(DefaultShrinkingPeriod);
                m_OnDisposeHandler += SubscribeAutoShrinkTime(AutoShrinkTimestamp);
            }
        }

        private T CreateInstance()
        {
            return m_Config.OnCreate();
        }

        ~ConfigurablePool()
        {
#if DEBUG
            Debug.LogError($"ConfigurablePool<{typeof(T)}> is not disposed properly. Use Dispose method to release resources.");
#endif
            DisposeFromMainThread().Forget();
        }

        /// <summary>
        /// Shrink time event handler
        /// </summary>
        public void AutoShrinkTimestamp(ShrinkTimestampType type)
        {
            switch (type)
            {
                case ShrinkTimestampType.Analysis:
                    m_FruitBasket.SetCount(OutOfPoolCount, Time.time);
#if DEBUG
                    Console.Verbose("ConfigurablePool", $"ConfigurablePool<{typeof(T)}> analysis event. Current in pool: {m_ObjectsInPool.Count}, Current out of pool: {OutOfPoolCount}, Max count: {m_FruitBasket.GetMaxCount()}");
#endif
                    break;
                case ShrinkTimestampType.Shrinking:
                    var maxCount = m_FruitBasket.GetMaxCount();
#if DEBUG
                    Console.Verbose("ConfigurablePool", $"ConfigurablePool<{typeof(T)}> shrinking. Max count for the period is {maxCount} (Current in pool: {m_ObjectsInPool.Count}, Current out of pool: {OutOfPoolCount})");
#endif
                    var countInPool = m_ObjectsInPool.Count;
                    var countToRemove = (countInPool + OutOfPoolCount) - maxCount;
                    if (countToRemove > 0 && countToRemove >= countInPool)
                    {
#if DEBUG
                        Console.Verbose("ConfigurablePool", $"ConfigurablePool<{typeof(T)}> decided to release {countToRemove} objects from pool");
#endif
                        for (var i = 0; i < countToRemove; i++)
                        {
                            if (m_ObjectsInPool.Count > 0)
                            {
                                var obj = m_ObjectsInPool.Dequeue();
                                if (_InstanceToPool.ContainsKey(obj))
                                {
                                    _InstanceToPool.Remove(obj);
                                }
                                m_Config.OnDestroy(obj);
                            }
                        }
                    }

                    if (maxCount == 0 && m_ObjectsInPool.Count == 0 && OutOfPoolCount == 0)
                    {
#if DEBUG
                        Console.Verbose("ConfigurablePool", $"ConfigurablePool<{typeof(T)}> is useless. Triggering OnPoolIsUseless event");
#endif
                        OnPoolIsUseless?.Invoke();
                    }

                    break;
            }
        }

        /// <summary>
        /// Get object from pool. If pool is empty or forceCreate is true, new object will be created
        /// </summary>
        public T Get(bool forceCreate = false)
        {
            T obj;
            if (m_ObjectsInPool.Count == 0 || forceCreate)
            {
                obj = CreateInstance();
            }
            else
            {
                obj = m_ObjectsInPool.Dequeue();
            }
            m_OutOfPool.Add(obj);
            m_Config.OnGet(obj);
            return obj;
        }

        /// <summary>
        /// Return object back to pool
        /// </summary>
        public void Return(T obj)
        {
            if (m_ObjectsInPool.Contains(obj))
            {
                Console.Error("ConfigurablePool", "Object is already in pool");
                return;
            }
#if DEBUG
            /* Check that returned object is contained in out of pool list */
            if (!m_OutOfPool.Contains(obj))
            {
                Console.Error("ConfigurablePool", "Object does not belong to this pool");
                return;
            }
#endif
            m_OutOfPool.Remove(obj);
            if (m_ObjectsInPool.Count < m_Limit)
            {
                m_Config.OnReturn(obj);
                m_ObjectsInPool.Enqueue(obj);
            }
            else
            {
#if DEBUG
                Debug.LogWarning("Pool limit reached. Object will be destroyed");
#endif
                m_Config.OnDestroy(obj);
            }
        }

        /// <summary>
        /// Determines whether object belongs to pool (was created by pool and wasn't released or detached)
        /// </summary>
        public bool IsBelongsToPool(T obj)
        {
            return m_OutOfPool.Contains(obj) || m_ObjectsInPool.Contains(obj);
        }

        /// <summary>
        /// Determines whether object is in pool
        /// </summary>
        public bool IsInPool(T obj)
        {
            return m_ObjectsInPool.Contains(obj);
        }

        /// <summary>
        /// Release object. Released object will not return to the pool.
        /// </summary>
        public void Release(T obj)
        {
#if DEBUG
            /* Check released object is not present in pool */
            if (m_ObjectsInPool.Contains(obj))
            {
                Debug.LogError("Object is in pool, you cannot release. Get it from pool first.");
                return;
            }
#endif
            /* Check that released object exists outside the pool */
            if (!m_OutOfPool.Contains(obj))
            {
                Debug.LogError("Object does not belong to this pool");
                return;
            }

            m_OutOfPool.Remove(obj);
            if (_InstanceToPool.ContainsKey(obj))
            {
                _InstanceToPool.Remove(obj);
            }
            m_Config.OnDestroy(obj);
        }

        /// <summary>
        /// Heavy operation! If you need to detach from the pool a specific object even if it's in the pool right now, use this method.
        /// </summary>
        public void HardDetach(T obj)
        {
            if (m_OutOfPool.Contains(obj))
            {
                m_OutOfPool.Remove(obj);
            }
            else if (m_ObjectsInPool.Contains(obj))
            {
                /* Remove the object from the pool */
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
            if (_InstanceToPool.ContainsKey(obj))
            {
                _InstanceToPool.Remove(obj);
            }
        }

        private async UniTaskVoid DisposeFromMainThread()
        {
            await UniTask.SwitchToMainThread();
            Dispose();
        }

        public void Dispose()
        {
            /* Dispose all objects in pool */
            foreach (var obj in m_ObjectsInPool)
            {
                m_Config.OnDestroy(obj);
            }
            /* Dispose all objects out of pool */
            foreach (var obj in m_OutOfPool)
            {
                if (_InstanceToPool.ContainsKey(obj))
                {
                    _InstanceToPool.Remove(obj);
                }
                m_Config.OnDestroy(obj);
            }
            m_ObjectsInPool.Clear();
            m_OutOfPool.Clear();
            m_OnDisposeHandler?.Invoke();
            GC.SuppressFinalize(this);
        }
    }
}
