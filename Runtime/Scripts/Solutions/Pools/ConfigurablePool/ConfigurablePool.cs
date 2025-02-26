using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Metamatic.DevTools;
using UnityEngine;

namespace Metamatic
{
    public abstract class ConfigurablePool: IDevPool
    {
#if DEBUG
        public static int PoolsCount;
        public static int SharedPoolsCount;
        public static readonly List<ConfigurablePool> Pools = new();
        public static event Action<ConfigurablePool> OnPoolAdded;
        public static event Action<ConfigurablePool> OnPoolRemoved;
        
        public static void RegisterPool(ConfigurablePool pool)
        {
            Pools.Add(pool);
            PoolsCount++;
            OnPoolAdded?.Invoke(pool);
        }
        
        public static void UnregisterPool(ConfigurablePool pool)
        {
            Pools.Remove(pool);
            PoolsCount--;
            OnPoolRemoved?.Invoke(pool);
        }

        static ConfigurablePool()
        {
            Monitoring.Create("ConfigurablePool", "Total count", () => PoolsCount, PlayerLoopTiming.FixedUpdate);
            Monitoring.Create("ConfigurablePool", "Shared count", () => SharedPoolsCount, PlayerLoopTiming.FixedUpdate);
        }
        public abstract string displayName { get; }
        public abstract int countInPool { get; }
        public abstract int countOutPool { get; }
#endif
    }

    /// <summary>
    /// The pool of any objects. The pool can be configured with the rules of object creation, destruction, and recycling.
    /// To create a pool, you need to implement the IPoolConfig interface and pass it to the pool constructor.
    /// Use existing configuration (like ComponentPrefabPoolConfig or GameObjectConfigurablePoolConfig) for the pool or create a new one.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ConfigurablePool<T> : ConfigurablePool, IDisposable
    {
        #region GlobalPool

        private static readonly Dictionary<object, ConfigurablePool<T>> _SharedPools = new();
        private static readonly Dictionary<object, ConfigurablePool<T>> _InstanceToPool = new();

        public static Summoner<T> GetSharedSummoner(IPoolConfig<T> config)
        {
            var pool = GetSharedPool(config);
            return new Summoner<T>(pool.Summon, pool.GetHashCode());
        }
        
        public static Summon<T> SummonShared(IPoolConfig<T> config, bool forceCreate = false)
        {
            var pool = GetSharedPool(config);
            var (instance, isNew, id) = pool.GetDetailed(forceCreate);
            _InstanceToPool[instance] = pool;
            return new Summon<T>(id, instance, pool.Return);
        }

        private static ConfigurablePool<T> GetSharedPool(IPoolConfig<T> config)
        {
            var key = config.GetKey();
            if (!_SharedPools.TryGetValue(key, out var pool))
            {
                pool = new ConfigurablePool<T>(config, autoShrink: true, capacity: 0);
                _SharedPools[key] = pool;
                #if DEBUG
                ConfigurablePool.SharedPoolsCount++;
                #endif
                pool.OnPoolIsUseless += () =>
                {
                    #if DEBUG
                    ConfigurablePool.SharedPoolsCount--;
                    #endif
                    /* Remove useless pool */
                    Debug.Log($"[POOL] Global pool of rules {config} is useless. Removing from global pool");
                    pool.Dispose();
                    _SharedPools.Remove(key);
                };
            }
            return pool;
        }

#if DEBUG
        public static int DebugGetCountInSharedPool(IPoolConfig<T> config)
        {
            var key = config.GetKey();
            if (_SharedPools.TryGetValue(key, out var pool))
            {
                return pool.InPoolCount;
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
                    var (instance, isNew, id) = pool.GetDetailed(true);
                    pool.Return(id);
                }
            }
        }
        
        /// <summary>
        /// Create in shared pool specified count of objects and keep them in the pool until dispose
        /// </summary>
        public static DisposableAction HardPreWarmShared(IPoolConfig<T> config, int count, bool lazy = false)
        {
            var pool = GetSharedPool(config);

            /* Set count of objects in pool to the fruit basket to keep them until dispose */
            var freezer = pool.m_FruitBasket?.AddFrozenCount(count);

            if (Application.isPlaying && lazy)
            {
                LazyPreWarmPool(pool, count).Forget();
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    var (instance, isNew, id) = pool.GetDetailed(true);
                    pool.Return(id);
                }
            }

            return new DisposableAction(() =>
            {
                freezer?.Dispose();
            });
        }

        public static async UniTaskVoid LazyPreWarmPool(ConfigurablePool<T> pool, int count)
        {
            for (int i = 0; i < count; i++)
            {
                var (instance, isNew, id) = pool.GetDetailed(true);
                pool.Return(id);
                await UniTask.NextFrame();
            }
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
        public static float DefaultShrinkingPeriod = 60f;
        public static float DefaultAnalysisPeriod = 10f;
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

        private readonly List<T> m_ObjectsInPoolList = new();
        private readonly HashSet<T> m_ObjectsInPoolSet = new();
        private readonly HashSet<T> m_OutOfPool = new();
        private readonly SignedIndexedPool<T> m_OutOfPoolIndexed = new(1000); // TODO: Are you sure about 1000?

        private readonly IPoolConfig<T> m_Config;
        private int m_Capacity;
        private int m_Limit = int.MaxValue;
        private bool m_AutoShrink;
        private AverageFruitBasket m_FruitBasket;
        private Action m_OnDisposeHandler;

        /// <summary>
        /// Count of objects in pool (available for get)
        /// </summary>
        public int InPoolCount => m_ObjectsInPoolList.Count;

        /// <summary>
        /// Count of objects out of pool (not available for get, but still belongs to pool)
        /// </summary>
        public int OutOfPoolCount => m_OutOfPoolIndexed.Count;

        /// <summary>
        /// The event is called when the pool has no objects in it and there's no objects out of pool
        /// </summary>
        public event Action OnPoolIsUseless;

        public ConfigurablePool(IPoolConfig<T> config, int capacity = 0, int limit = Int32.MaxValue, bool autoShrink = false)
        {
            m_Config = config;
#if DEBUG
            RegisterPool(this);
            m_OnDisposeHandler += () =>
            {
                UnregisterPool(this);
            };
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
                m_ObjectsInPoolList.Add(instance);
                m_ObjectsInPoolSet.Add(instance);
                m_Config.OnReturn(instance);
            }

            if (autoShrink)
            {
                /* Start auto shrink timer */
                m_FruitBasket = new AverageFruitBasket(DefaultShrinkingPeriod);
#if !DISABLE_POOL_AUTOSHRINK
                m_OnDisposeHandler += SubscribeAutoShrinkTime(AutoShrinkTimestamp);
                #endif
            }
        }
        
        public void AddCapacity(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var instance = CreateInstance();
                m_ObjectsInPoolList.Add(instance);
                m_ObjectsInPoolSet.Add(instance);
                m_Config.OnReturn(instance);
            }
            m_Capacity += count;
        }

        public void WithdrawCapacity(int count)
        {
            var countToRemove = Math.Min(count, m_ObjectsInPoolList.Count);
            for (int i = 0; i < countToRemove; i++)
            {
                var lastIndex = m_ObjectsInPoolList.Count - 1;
                var obj = m_ObjectsInPoolList[lastIndex];
                m_ObjectsInPoolList.RemoveAt(lastIndex);
                m_ObjectsInPoolSet.Remove(obj);
                m_Config.OnDestroy(obj);
            }

            m_Capacity = Mathf.Max(m_Capacity - count, 0);

            if (m_Capacity == 0 && InPoolCount == 0 && m_OutOfPool.Count == 0)
            {
                OnPoolIsUseless?.Invoke();
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
                    break;
                case ShrinkTimestampType.Shrinking:
                    var maxCount = Mathf.Max(m_Capacity, m_FruitBasket.GetMaxCount());
#if DEBUG
                    Console.Verbose("ConfigurablePool", $"ConfigurablePool<{typeof(T)}> shrinking. Max count for the period is {maxCount} (Current in pool: {m_ObjectsInPoolList.Count}, Current out of pool: {OutOfPoolCount})");
#endif
                    var countInPool = m_ObjectsInPoolList.Count;
                    var countToRemove = (countInPool + OutOfPoolCount) - maxCount;
                    if (countToRemove > 0 && countToRemove >= countInPool)
                    {
#if DEBUG
                        Console.Verbose("ConfigurablePool", $"ConfigurablePool<{typeof(T)}> decided to release {countToRemove} objects from pool");
#endif
                        for (var i = 0; i < countToRemove; i++)
                        {
                            if (m_ObjectsInPoolList.Count > 0)
                            {
                                var lastIndex = m_ObjectsInPoolList.Count - 1;
                                var obj = m_ObjectsInPoolList[lastIndex];
                                m_ObjectsInPoolList.RemoveAt(lastIndex);
                                m_ObjectsInPoolSet.Remove(obj);
                                
                                if (_InstanceToPool.ContainsKey(obj))
                                {
                                    _InstanceToPool.Remove(obj);
                                }
                                m_Config.OnDestroy(obj);
                            }
                        }
                    }

                    if (maxCount == 0 && m_ObjectsInPoolList.Count == 0 && OutOfPoolCount == 0)
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
        public (T instance, Token96Bit token) Get(bool forceCreate = false)
        {
            T obj;
            if (m_ObjectsInPoolList.Count == 0 || forceCreate)
            {
                obj = CreateInstance();
            }
            else
            {
                var lastIndex = m_ObjectsInPoolList.Count - 1;
                obj = m_ObjectsInPoolList[lastIndex];
                m_ObjectsInPoolList.RemoveAt(lastIndex);
                m_ObjectsInPoolSet.Remove(obj);
            }
            m_OutOfPool.Add(obj);
            m_Config.OnGet(obj);
            var id = m_OutOfPoolIndexed.Add(obj);
            return (obj, id);
        }
        
        public Summon<T> Summon()
        {
            var (instance, id) = Get();
            return new Summon<T>(id, instance, Return);
        }
        
        public Summoner<T> GetSummoner()
        {
            return new Summoner<T>(Summon, GetHashCode());
        }
        
        /// <summary>
        /// Get object from pool. If pool is empty or forceCreate is true, new object will be created
        /// </summary>
        public (T instance, bool isNew, Token96Bit token) GetDetailed(bool forceCreate = false)
        {
            T obj;
            bool isNew = false;
            if (m_ObjectsInPoolList.Count == 0 || forceCreate)
            {
                obj = CreateInstance();
                isNew = true;
            }
            else
            {
                var lastIndex = m_ObjectsInPoolList.Count - 1;
                obj = m_ObjectsInPoolList[lastIndex];
                m_ObjectsInPoolList.RemoveAt(lastIndex);
                m_ObjectsInPoolSet.Remove(obj);
            }
            m_OutOfPool.Add(obj);
            m_Config.OnGet(obj);
            var id = m_OutOfPoolIndexed.Add(obj);
            return (obj, isNew, id);
        }

        public void Return(Token96Bit token)
        {
            if (m_OutOfPoolIndexed.Remove(token, out var obj))
            {
                m_OutOfPool.Remove(obj);
                if (m_ObjectsInPoolList.Count < m_Limit)
                {
                    m_Config.OnReturn(obj);
                    m_ObjectsInPoolList.Add(obj);
                    m_ObjectsInPoolSet.Add(obj);
                }
                else
                {
#if DEBUG
                    Debug.LogWarning("Pool limit reached. Object will be destroyed");
#endif
                    m_Config.OnDestroy(obj);
                }
            }
            else
            {
                Console.Error("ConfigurablePool", "Object does not belong to this pool");
            }
        }

        /// <summary>
        /// Determines whether object belongs to pool (was created by pool and wasn't released or detached)
        /// </summary>
        public bool IsBelongsToPool(T obj)
        {
            return m_OutOfPool.Contains(obj) || m_ObjectsInPoolSet.Contains(obj);
        }

        /// <summary>
        /// Determines whether object is in pool
        /// </summary>
        public bool IsInPool(T obj)
        {
            return m_ObjectsInPoolSet.Contains(obj);
        }

        /// <summary>
        /// Release object. Released object will not return to the pool.
        /// </summary>
        public void Release(T obj)
        {
#if DEBUG
            /* Check released object is not present in pool */
            if (m_ObjectsInPoolSet.Contains(obj))
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
            else if (m_ObjectsInPoolSet.Contains(obj))
            {
                /* Remove the object from the pool */
                if (m_ObjectsInPoolSet.Remove(obj))
                {
                    m_ObjectsInPoolList.Remove(obj);
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
            foreach (var obj in m_ObjectsInPoolList)
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
            m_ObjectsInPoolList.Clear();
            m_ObjectsInPoolSet.Clear();
            m_OutOfPool.Clear();
            m_OnDisposeHandler?.Invoke();
            GC.SuppressFinalize(this);
        }
        
        #if DEBUG
        public override string displayName => $"ConfigurablePool<{typeof(T).Name}> {m_Config.GetKey()}";
        public override int countInPool => m_ObjectsInPoolList.Count;
        public override int countOutPool => m_OutOfPool.Count;
        #endif
    }
}
