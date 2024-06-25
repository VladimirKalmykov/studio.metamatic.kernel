using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

namespace Metamatic
{
    public class GameObjectPool: IObjectPool<GameObject>, IDisposable
    {
        private readonly ObjectPool<GameObject> m_Pool;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="prefab"></param>
        /// <param name="parentTransform"></param>
        /// <param name="collectionCheck">Set false for best performance, true for more accuracy</param>
        /// <param name="defaultCapacity"></param>
        /// <param name="maxSize"></param>
        public GameObjectPool(Func<GameObject> createFunc, bool collectionCheck = false, int defaultCapacity = 10, int maxSize = 10000)
        {
            m_Pool = new ObjectPool<GameObject>(
                createFunc: () =>
                {
                    var instance = createFunc();
                    instance.SetActive(false);
                    instance.AddComponent<ObjectFromPool>().SetReleaseAction(() =>
                    {
                        if (m_Pool != null)
                        {
#if DEBUG
                            try
                            {
#endif
                                m_Pool.Release(instance);
#if DEBUG
                            }
                            catch (Exception e)
                            {
                                Debug.LogError($"Release object {instance.name} from pool error");
                                Debug.LogError(e);
                            }
 #endif
                        }
                    });
                    return instance;
                },
                actionOnRelease: dialog =>
                {
                    dialog.gameObject.SetActive(false);
                },
                actionOnDestroy: dialog =>
                {
                    var objectFromPool = dialog.gameObject.GetComponent<ObjectFromPool>();
                    #if DEBUG
                    if (objectFromPool == null)
                    {
                        throw new Exception($"Object {dialog.name} does not have ObjectFromPool component");
                    }
                    #endif
                    Object.Destroy(objectFromPool);
                    Object.Destroy(dialog.gameObject);
                },
                collectionCheck: collectionCheck,
                defaultCapacity: defaultCapacity,
                maxSize: maxSize);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GameObject Get()
        {
            return m_Pool.Get();
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PooledObject<GameObject> Get(out GameObject v)
        {
            return m_Pool.Get(out v);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Release(GameObject element)
        {
            m_Pool.Release(element);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            m_Pool.Clear();
        }

        public int CountInactive => m_Pool.CountInactive;
        public void Dispose()
        {
            Debug.Log("Dispose pull");
            Clear();
            m_Pool?.Dispose();
        }
    }
}