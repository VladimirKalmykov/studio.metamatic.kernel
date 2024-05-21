using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

namespace Metamatic
{
    public class BehaviourPrefabPool<T>: IObjectPool<T>, IDisposable
        where T: MonoBehaviour
    {
        private readonly ObjectPool<T> m_Pool;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="prefab"></param>
        /// <param name="parentTransform"></param>
        /// <param name="collectionCheck">Set false for best performance, true for more accuracy</param>
        /// <param name="defaultCapacity"></param>
        /// <param name="maxSize"></param>
        public BehaviourPrefabPool(T prefab, bool collectionCheck = true, int defaultCapacity = 10, int maxSize = 10000)
        {
            m_Pool = new ObjectPool<T>(
                createFunc: () =>
                {
                    var instance = UnityEngine.Object.Instantiate(prefab);
                    instance.gameObject.SetActive(false);
                    instance.gameObject.AddComponent<ObjectFromPool>().SetReleaseAction(() =>
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
                        instance.gameObject.SetActive(false);
                    });
                    return instance;
                },
                actionOnRelease: dialog =>
                {
                    dialog.gameObject.SetActive(false);
                },
                actionOnDestroy: dialog =>
                {
                    Object.Destroy(dialog.gameObject);
                },
                collectionCheck: collectionCheck,
                defaultCapacity: defaultCapacity,
                maxSize: maxSize);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Get()
        {
            return m_Pool.Get();
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PooledObject<T> Get(out T v)
        {
            return m_Pool.Get(out v);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Release(T element)
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
            Clear();
            m_Pool?.Dispose();
        }
    }
}