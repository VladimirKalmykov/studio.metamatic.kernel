using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic
{
    public abstract class Pool: IPool
    {
        #region Static
        private static Dictionary<object, Pool> m_Pools = new ();
#if DEBUG
        public string debugUniqueId { get; protected set;  }
#endif

        /// <summary>
        /// Get a pool for the specified component prefab. Dispose the pool when it is no longer needed (required).
        /// </summary>
        /// <param name="obj"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IPool<T> GetComponentPool<T>(T obj) where T: Component
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            if (m_Pools.TryGetValue(obj, out var pool))
            {
                return new PoolReserve<T>(pool as OrganicPool<T>);
            }
            else
            {
                pool = new OrganicPool<T>(
                    onCreate: () => UnityEngine.Object.Instantiate(obj),
                    onRelease: (instance) => instance.gameObject.SetActive(false),
                    onGet: (instance) => instance.gameObject.SetActive(true),
                    onDestroy: (instance) => UnityEngine.Object.Destroy(instance.gameObject)
                );
                pool.OnReadyToRelease += () =>
                {
                    m_Pools.Remove(obj);
                    pool.Dispose();
                };
                m_Pools.Add(obj, pool);
                return new PoolReserve<T>(pool as OrganicPool<T>);
            }
        }
        
        public static IPool<GameObject> GetGameObjectPool(GameObject obj)
        {
            if (m_Pools.TryGetValue(obj, out var pool))
            {
                return new PoolReserve<GameObject>(pool as OrganicPool<GameObject>);
            }
            else
            {
                pool = new OrganicPool<GameObject>(() => UnityEngine.Object.Instantiate(obj), onDestroy: (instance) => UnityEngine.Object.Destroy(instance));
                pool.OnReadyToRelease += () =>
                {
                    m_Pools.Remove(obj);
                    pool.Dispose();
                };
                m_Pools.Add(obj, pool);
                return new PoolReserve<GameObject>(pool as OrganicPool<GameObject>);
            }
        }
        
        public static IPool<GameObject> GetGameObjectPool(Func<GameObject> onCreate)
        {
            if (m_Pools.TryGetValue(onCreate, out var pool))
            {
                return new PoolReserve<GameObject>(pool as OrganicPool<GameObject>);
            }
            else
            {
                pool = new OrganicPool<GameObject>(onCreate, onDestroy: (instance) => UnityEngine.Object.Destroy(instance));
                pool.OnReadyToRelease += () =>
                {
                    m_Pools.Remove(onCreate);
                    pool.Dispose();
                };
                m_Pools.Add(onCreate, pool);
                return new PoolReserve<GameObject>(pool as OrganicPool<GameObject>);
            }
        }
        #endregion
        
        protected int outOfPoolObjectsCount = 0;
        protected event Action OnAllObjectsReleased;
        public event Action OnReadyToRelease;
        protected readonly CancellationTokenSource m_DisposeCancellationTokenSource = new ();
        public CancellationToken disposeCancellationToken => m_DisposeCancellationTokenSource.Token;
        /// <summary>
        /// Wait until all created objects are released. It uses simple math to calculate spawned and released objects.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public UniTask WhenAllReleased(CancellationToken cancellationToken)
        {
            var tcs = new UniTaskCompletionSource();
            Action handler = null;
            CancellationTokenRegistration registration = default;
            handler = () =>
            {
                registration.Dispose();
                OnAllObjectsReleased -= handler;
                tcs.TrySetResult();
            };
            OnAllObjectsReleased += handler;
            registration = cancellationToken.Register(() =>
            {
                OnAllObjectsReleased -= handler;
                tcs.TrySetCanceled();
            });
            return tcs.Task;
        }

        public virtual void Dispose()
        {
            if (m_DisposeCancellationTokenSource.IsCancellationRequested) return;
            m_DisposeCancellationTokenSource.Cancel();
            GC.SuppressFinalize(this);
        }
        
        protected void ReadyToRelease()
        {
            OnReadyToRelease?.Invoke();
        }
        
        protected void AllObjectsReleased()
        {
            OnAllObjectsReleased?.Invoke();
        }
    }
    
    public class Pool<T>: Pool, IPool<T>
    where T: UnityEngine.Object
    {
        protected readonly Stack<T> objectsInPool;
        protected readonly LinkedList<T> allProducedObjects = new ();
        private readonly Func<T> m_OnCreate;
        private readonly Action<T> m_OnGet;
        private readonly Action<T> m_OnRelease;
        protected readonly Action<T> onDestroy;
        protected readonly int capacity;
        
        public Pool(Func<T> onCreate, Action<T> onGet = null, Action<T> onRelease = null, Action<T> onDestroy = null, int capacity = 1)
        {
            objectsInPool = new Stack<T>(capacity);
            m_OnCreate = onCreate;
            m_OnGet = onGet;
            m_OnRelease = onRelease;
            this.onDestroy = onDestroy ?? DefaultDestroy;
            this.capacity = capacity;
            debugUniqueId = Guid.NewGuid().ToString();
        }
        
        public virtual T Get()
        {
            #if DEBUG
            if (m_DisposeCancellationTokenSource.IsCancellationRequested) throw new ObjectDisposedException(nameof(Pool<T>));
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

        private List<T> m_ReleaseHistory = new ();
        public virtual void Release(T obj)
        {
#if DEBUG
            if (m_DisposeCancellationTokenSource.IsCancellationRequested) throw new ObjectDisposedException(nameof(Pool<T>));
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
            Release(obj);
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
    
    public class OrganicPool<T>: Pool<T> where T: UnityEngine.Object {
        
        private CancellationTokenSource m_LifetimeCancellationTokenSource = new ();
        private float m_Lifetime;
        private int m_InquiryCount;
        private int m_CountInPoolAtCountdownStart = 0;
        private bool m_IsCountingDown = false;
        
        internal OrganicPool(Func<T> onCreate, Action<T> onGet = null, Action<T> onRelease = null, Action<T> onDestroy = null, int capacity = 1, float lifetime = 30f) : base(onCreate, onGet, onRelease, onDestroy, capacity)
        {   
            m_Lifetime = lifetime;
        }
        
        public DisposableAction Inquire()
        {
            m_InquiryCount++;
            bool disposed = false;
            return new DisposableAction(() =>
            {
                if (disposed) return;
                disposed = true;
                m_InquiryCount--;
                if (m_InquiryCount == 0)
                {
                    m_LifetimeCancellationTokenSource.Cancel();
                    ReadyToRelease();
                }
            });
        }
        
        public override T Get()
        {
            m_CountInPoolAtCountdownStart--;
            T obj = base.Get();
            return obj;
        }
        
        public override void Release(T obj)
        {
            m_CountInPoolAtCountdownStart++;
            base.Release(obj);
            /* Start the lifetime countdown */
            StartCountdown();
        }
        
        private async void StartCountdown()
        {
            if (m_IsCountingDown) return;
            m_CountInPoolAtCountdownStart = objectsInPool.Count;
            m_IsCountingDown = true;
            var shouldRestart = false;
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(m_Lifetime),
                    cancellationToken: m_LifetimeCancellationTokenSource.Token);
                if (objectsInPool.Count > capacity)
                {
                    if (m_CountInPoolAtCountdownStart <= 0) return;
                    var countToDelete = Mathf.Min(objectsInPool.Count - capacity, m_CountInPoolAtCountdownStart);
                    /* Remove countToDelete objects from the pool */
                    var countToDeleteClamped = Mathf.Clamp(countToDelete, 0, objectsInPool.Count - capacity);
                    for (int i = 0; i < countToDeleteClamped; i++)
                    {
                        T obj = objectsInPool.Pop();
                        Destroy(obj);
                    }

                    if (objectsInPool.Count > capacity)
                    {
                        /* Restart the countdown */
                        shouldRestart = true;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                /* Ignore */
            }
            catch (Exception e)
            {
                Debug.LogError($"Countdown error: {e.Message}");
                Debug.LogError(e);
            }
            finally
            {
                m_IsCountingDown = false;
                if (shouldRestart)
                {
                    StartCountdown();
                }
            }
        }
    }

    public readonly struct PoolReserve<T> : IPool<T>
        where T : UnityEngine.Object
    {
        private readonly OrganicPool<T> m_Pool;
        private readonly IDisposable m_Reserve;
        public PoolReserve(OrganicPool<T> pool)
        {
            m_Pool = pool;
            m_Reserve = m_Pool.Inquire();
        }

        public string debugUniqueId => m_Pool.debugUniqueId;
        public CancellationToken disposeCancellationToken => m_Pool.disposeCancellationToken;
        public UniTask WhenAllReleased(CancellationToken cancellationToken)
        {
            return m_Pool.WhenAllReleased(cancellationToken);
        }

        public T Get()
        {
            return m_Pool.Get();
        }

        public void Release(T obj)
        {
            m_Pool.Release(obj);
        }

        public void TryRelease(T obj)
        {
            m_Pool.TryRelease(obj);
        }

        public void Dispose()
        {
            m_Reserve.Dispose();
        }
    }
}