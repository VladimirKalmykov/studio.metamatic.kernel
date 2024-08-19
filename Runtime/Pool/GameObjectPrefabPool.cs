using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

namespace Metamatic
{
    [Obsolete("Use Pool.GetOrganicGameObjectPool instead")]
    public class GameObjectPrefabPool: IObjectPool<GameObject>, IDisposable
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
        public GameObjectPrefabPool(GameObject prefab, bool collectionCheck = true, int defaultCapacity = 10, int maxSize = 10000, bool supportObjectFromPool = false)
        {
            m_Pool = new ObjectPool<GameObject>(
                createFunc: () =>
                {
                    var instance = UnityEngine.Object.Instantiate(prefab);
                    instance.gameObject.SetActive(false);
                    if (supportObjectFromPool)
                    {
                        instance.gameObject.AddComponent<ObjectFromPool>().SetReleaseAction(() =>
                        {
                            m_Pool?.Release(instance);
                            instance.gameObject.SetActive(false);
                        });
                    }
                    instance.gameObject.TryAddComponent<DisposableBehaviour>().AddDisposeListenerOnce(() =>
                    {
                        m_Pool?.Release(instance);
                        instance.gameObject.SetActive(false);
                    });
                    return instance;
                },
                actionOnRelease: go =>
                {
                    if (go == null)
                        return;
                    go.SetActive(false);
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
            Clear();
            m_Pool?.Dispose();
        }
    }
}