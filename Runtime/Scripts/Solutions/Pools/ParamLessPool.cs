using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Metamatic.DevTools;
using UnityEngine;

namespace Metamatic
{
    public abstract class ParamLessPool: IDevPool
    {
#if DEBUG
        public static int PoolsCount;
        public static int SharedPoolsCount;
        public static List<ParamLessPool> Pools = new();
        public static event Action<ParamLessPool> OnPoolAdded;
        public static event Action<ParamLessPool> OnPoolRemoved;
        
        public static void RegisterPool(ParamLessPool pool)
        {
            Pools.Add(pool);
            PoolsCount++;
            OnPoolAdded?.Invoke(pool);
        }
        
        public static void UnregisterPool(ParamLessPool pool)
        {
            Pools.Remove(pool);
            PoolsCount--;
            OnPoolRemoved?.Invoke(pool);
        }
        
        public abstract string displayName { get; }
        public abstract int countInPool { get; }
        public abstract int countOutPool { get; }

        static ParamLessPool()
        {
            Monitoring.Create("ParamLessPool", "Total count", () => PoolsCount, PlayerLoopTiming.FixedUpdate);
            Monitoring.Create("ParamLessPool", "Shared count", () => SharedPoolsCount, PlayerLoopTiming.FixedUpdate);
        }
#endif
        
        public static event Action OnUnityInitialized;
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void OnUnityInitializedHandler()
        {
            OnUnityInitialized?.Invoke();
            OnUnityInitialized = null;
        }
    }

    public class ParamLessPool<T> : ParamLessPool, IDisposable
    where T: new ()
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
                #if DEBUG
                ParamLessPool.SharedPoolsCount++;
                #endif
                pool.OnPoolIsUseless += () =>
                {
                    #if DEBUG
                    ParamLessPool.SharedPoolsCount--;
                    #endif
                    /* Remove useless pool */
                    global::Metamatic.Console.Verbose("Pool", "ParamLessPool", $"Global pool of type {typeof(T)} is useless. Removing from global pool");
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
        
        #if DEBUG
        public static int DebugGetCountInSharedPool()
        {
            if (_SharedPool == null)
            {
                return 0;
            }

            return _SharedPool.InPoolCount;
        }
        
        public static int DebugGetCountOutOfSharedPool()
        {
            if (_SharedPool == null)
            {
                return 0;
            }

            return _SharedPool.OutOfPoolCount;
        }
        #endif

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
        public static float DefaultShrinkingPeriod = 60f;

        // ReSharper disable once FieldCanBeMadeReadOnly.Global
        // ReSharper disable once StaticMemberInGenericType
        // ReSharper disable once MemberCanBePrivate.Global
        public static float DefaultAnalysisPeriod = 30f;
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
                #if METAMATIC_VERBOSE_LOG
                Console.Verbose("ParamLessPool", "AutoShrink", $"Enabling background tracking.");
                #endif
                EnableBackgroundTracking();
            }
#if METAMATIC_VERBOSE_LOG
            Console.Verbose("ParamLessPool", "AutoShrink", $"Subscribed to auto shrink event. Current subscribers: {_BackgroundTrackingInquiryCount}");
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
                Console.Error("ParamLessPool", "AutoShrink", "Cannot enable auto shrink for global pool twice");
                return;
            }

            _BackgroundTimeIsEnabled = true;
            m_BackgroundRecheckTimer = new Timer((state) => InternalOnBackgroundTrack(state).Forget(), null,
                (int)(DefaultAnalysisPeriod * 1000), (int)(DefaultAnalysisPeriod * 1000));
        }

        private void DisableBackgroundTracking()
        {
#if METAMATIC_VERBOSE_LOG
            Console.Verbose("ParamLessPool", "AutoShrink", $"Disabling background tracking.");
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
        private HashSet<T> m_OutOfPool = new();

        private int m_Capacity;
        private int m_Limit = int.MaxValue;
        private bool m_AutoShrink;
        private AverageFruitBasket m_FruitBasket;
        private Action m_OnDisposeHandler;

        /// <summary>
        /// Count of objects in pool (available for get)
        /// </summary>
        // ReSharper disable once InconsistentNaming
        public int InPoolCount => m_ObjectsInPoolList.Count;

        /// <summary>
        /// Count of objects out of pool (not available for get, but still belongs to pool)
        /// </summary>
        // ReSharper disable once InconsistentNaming
        public int OutOfPoolCount => m_OutOfPool.Count;
        
        #if DEBUG
        public int[] DebugGetOutOfPoolHashes()
        {
            var hashes = new int[m_OutOfPool.Count];
            var i = 0;
            foreach (var ooop in m_OutOfPool)
            {
                hashes[i] = ooop.GetHashCode();
                i++;
            }

            return hashes;
        }
        #endif

        /// <summary>
        /// The event is called when the pool has no objects in it and there's no objects out of pool
        /// </summary>
        public event Action OnPoolIsUseless;


        public ParamLessPool()
        {
#if DEBUG
            RegisterPool(this);
            var thisPool = this;
            m_OnDisposeHandler += () =>
            {
                UnregisterPool(thisPool);
            };
#endif
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
                m_ObjectsInPoolList.Add(CreateInstance());
                m_ObjectsInPoolSet.Add(m_ObjectsInPoolList[i]);
            }

            if (autoShrink)
            {
                OnUnityInitialized += LaunchAutoShrinkOnGameStart;
            }
        }

        private void LaunchAutoShrinkOnGameStart()
        {
            /* Start auto shrink timer */
            m_FruitBasket = new AverageFruitBasket(DefaultShrinkingPeriod);
#if !DISABLE_POOL_AUTOSHRINK
            m_OnDisposeHandler += SubscribeAutoShrinkTime(AutoShrinkTimestamp);
#endif
        }
        
        private T CreateInstance()
        {
            return new T();
        }

        ~ParamLessPool()
        {
            #if DEBUG
            Console.Error("ParamLessPool", $"ParamLessPool<${typeof(T)}> is not disposed properly. Use Dispose method to release resources.");
            #endif
            DisposeFromMainThread().Forget();
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
                    #if DEBUG
                    Console.Verbose("ParamLessPool", "AutoShrink", $"ParamLessPool<${typeof(T)}> analysis event. Current in pool: {m_ObjectsInPoolList.Count}, Current out of pool: {OutOfPoolCount}, Max count: {m_FruitBasket.GetMaxCount()}");
                    #endif
                    break;
                case ShrinkTimestampType.Shrinking:
                    var maxCount = m_FruitBasket.GetMaxCount();
                    #if DEBUG
                    Console.Verbose("ParamLessPool", "AutoShrink", $"ParamLessPool<${typeof(T)}> shrinking. Max count for the period is {maxCount} (Current in pool: {m_ObjectsInPoolList.Count}, Current out of pool: {OutOfPoolCount})");
                    #endif
                    var countInPool = m_ObjectsInPoolList.Count;
                    var countToRemove = (countInPool + OutOfPoolCount) - maxCount;
                    if (countToRemove > 0 && countToRemove >= countInPool)
                    {
                        #if DEBUG
                        Console.Verbose("ParamLessPool", "AutoShrink", $"ParamLessPool<${typeof(T)}> decided to release {countToRemove} objects from pool");
                        #endif
                        for (var i = 0; i < countToRemove; i++)
                        {
                            if (m_ObjectsInPoolList.Count > 0)
                            {
                                var lastIndex = m_ObjectsInPoolList.Count - 1;
                                var obj = m_ObjectsInPoolList[lastIndex];
                                m_ObjectsInPoolList.RemoveAt(lastIndex);
                                m_ObjectsInPoolSet.Remove(obj);
                                if (obj is IDisposable disposable)
                                {
                                    disposable.Dispose();
                                }
                            }
                        }
                    }

                    if (maxCount == 0 && m_ObjectsInPoolList.Count == 0 && OutOfPoolCount == 0)
                    {
                        #if DEBUG
                        Console.Verbose("ParamLessPool", "AutoShrink", $"ParamLessPool<${typeof(T)}> is useless. Triggering OnPoolIsEmpty event");
                        #endif
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
            var outOfPool = m_ObjectsInPoolList.Count == 0 || forceCreate ? CreateInstance() : Dequeue();
            m_OutOfPool.Add(outOfPool);
            if (outOfPool is IInitializable initializable)
            {
                initializable.Initialize();
            }
            return outOfPool;
        }
        
        private T Dequeue()
        {
            var listIndex = m_ObjectsInPoolList.Count - 1;
            var obj = m_ObjectsInPoolList[listIndex];
            m_ObjectsInPoolList.RemoveAt(listIndex);
            m_ObjectsInPoolSet.Remove(obj);
            return obj;
        }

        /// <summary>
        /// Return object back to pool
        /// </summary>
        /// <param name="obj"></param>
        /// <exception cref="Exception"></exception>
        public void Return(T obj)
        {
            if (m_ObjectsInPoolSet.Contains(obj))
            {
                global::Metamatic.Console.Error("ParamLessPool", "Object is already in pool");
                return;
            }
#if DEBUG
            /* Check that returned object is contained in out of pool list */
            if (!m_OutOfPool.Contains(obj))
            {
                Console.Error("ParamLessPool", $"Object ${obj} is not belong to this pool");
                return;
            }
#endif
            m_OutOfPool.Remove(obj);
            if (m_ObjectsInPoolList.Count < m_Limit)
            {
                // Auto reset object if it is resettable
                if (obj is IResettable resettable)
                {
                    resettable.Reset();
                }
                m_ObjectsInPoolList.Add(obj);
                m_ObjectsInPoolSet.Add(obj);
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
        public bool IsBelongsToPool(T obj)
        {
            return m_OutOfPool.Contains(obj) || m_ObjectsInPoolSet.Contains(obj);
        }
        
        /// <summary>
        /// Determines whether object is in pool
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public bool IsInPool(T obj)
        {
            return m_ObjectsInPoolSet.Contains(obj);
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
            if (m_ObjectsInPoolSet.Contains(obj))
            {
                global::Metamatic.Console.Error("ParamLessPool", "Object is in pool, you can not release. Get it from pool first.");
                return;
            }
#endif
            /* Check that released object is exists outside the pool */
            if (!m_OutOfPool.Contains(obj))
            {
                global::Metamatic.Console.Error("ParamLessPool", "Object is not belong to this pool");
                return;
            }

            if (m_OutOfPool.Contains(obj))
            {
                m_OutOfPool.Remove(obj);
            }
            
            if (obj is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        /// <summary>
        /// Heavy operation! If you need detach from the pool concrete object even if its in the pool right now, use this method.
        /// </summary>
        /// <param name="obj"></param>
        public void HardDetach(T obj)
        {
            if (m_OutOfPool.Contains(obj))
            {
                m_OutOfPool.Remove(obj);
            }
            else if (m_ObjectsInPoolSet.Contains(obj))
            {
                /* Create copy of queue */
                
                /* Remove the object from the pool */
                if (m_ObjectsInPoolSet.Remove(obj))
                {
                    m_ObjectsInPoolList.Remove(obj);
                }
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
                if (obj is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            m_ObjectsInPoolList.Clear();
            m_ObjectsInPoolSet.Clear();
            /* Dispose all objects out of pool */
            foreach (var obj in m_OutOfPool)
            {
                if (obj is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            m_OutOfPool.Clear();
            m_OnDisposeHandler?.Invoke();
            GC.SuppressFinalize(this);
        }
        
        #if DEBUG
        public override string displayName => $"ParamLessPool<{typeof(T).Name}>";
        public override int countInPool => InPoolCount;
        public override int countOutPool => OutOfPoolCount;
        #endif
    }
}