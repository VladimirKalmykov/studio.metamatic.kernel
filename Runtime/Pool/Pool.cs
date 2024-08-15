using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic
{
    /// <summary>
    /// Abstract and global class Pool is a base class for all pools. It has several static methods to get a pool of different types.
    /// </summary>
    public abstract class Pool: IPool
    {
        #region Static
        private static Dictionary<object, Pool> m_Pools = new ();
#if DEBUG
        public string debugUniqueId { get; protected set;  }
#endif

        /// <summary>
        /// Get a pool for the specified component prefab. Dispose the pool when it is no longer needed (required).
        /// Component game object will be disabled when released and enabled when get.
        /// </summary>
        /// <param name="obj"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IUnityObjectPool<T> GetOrganicComponentPool<T>(T obj) where T: Component
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
        
        public static IUnityObjectPool<GameObject> GetOrganicGameObjectPool(GameObject obj, int capacity = 1)
        {
            if (m_Pools.TryGetValue(obj, out var pool))
            {
                return new PoolReserve<GameObject>(pool as OrganicPool<GameObject>);
            }
            else
            {
                pool = new OrganicPool<GameObject>(() => UnityEngine.Object.Instantiate(obj), onDestroy: (instance) => UnityEngine.Object.Destroy(instance), capacity: capacity);
                pool.OnReadyToRelease += () =>
                {
                    m_Pools.Remove(obj);
                    pool.Dispose();
                };
                m_Pools.Add(obj, pool);
                return new PoolReserve<GameObject>(pool as OrganicPool<GameObject>);
            }
        }
        
        public static IUnityObjectPool<GameObject> GetOrganicGameObjectPool(Func<GameObject> onCreate)
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
            #if DEBUG
            if (m_DisposeCancellationTokenSource.IsCancellationRequested)
            {
                throw new ObjectDisposedException(nameof(Pool));
            }
            #endif
            m_DisposeCancellationTokenSource.Cancel();
            m_DisposeCancellationTokenSource.Dispose();
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
    
    /// <summary>
    /// Is a helper struct that allows you to reserve a pool from pools pool. Dispose the pool when it is no longer needed it will be returned to the pool.
    /// Do not use this struct outside the scope of the method where it was created.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public readonly struct PoolReserve<T> : IUnityObjectPool<T>
        where T : UnityEngine.Object
    {
        public readonly string ID; 
        private readonly OrganicPool<T> m_Pool;
        private readonly IDisposable m_Reserve;
        public PoolReserve(OrganicPool<T> pool)
        {
            ID = Guid.NewGuid().ToString();
            m_Pool = pool;
            m_Reserve = m_Pool.Inquire(ID);
        }
        
#if DEBUG
        public string debugUniqueId => m_Pool.debugUniqueId;
#endif
        public CancellationToken disposeCancellationToken => m_Pool.disposeCancellationToken;
        public UniTask WhenAllReleased(CancellationToken cancellationToken)
        {
            return m_Pool.WhenAllReleased(cancellationToken);
        }
        
        /// <summary>
        /// Set the capacity of the pool. After the pool reserve is disposed, the capacity will be reset to the default value.
        /// </summary>
        /// <param name="capacity"></param>
        public void SetCapacity(int capacity)
        {
            m_Pool.SetTemporaryCapacity(ID, capacity);
        }

        public T Get()
        {
            return m_Pool.Get();
        }

        public void Return(T obj)
        {
            m_Pool.Return(obj);
        }

        public void TryReturn(T obj)
        {
            m_Pool.TryReturn(obj);
        }

        public void Dispose()
        {
            m_Reserve.Dispose();
        }
    }
}