namespace Metamatic
{
    public class ComponentPrefabPoolConfig<T>: IPoolConfig<T>
        where T: UnityEngine.Component
    {
        public readonly T Prefab;
        private readonly bool m_KeepInactive;
        private readonly bool m_DontDestroyOnLoad;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="prefab"></param>
        /// <param name="keepInactive">Do not activate object on get from pool</param>
        public ComponentPrefabPoolConfig(T prefab, bool keepInactive = false, bool dontDestroyOnLoad = false)
        {
            Prefab = prefab;
            m_KeepInactive = keepInactive;
            m_DontDestroyOnLoad = dontDestroyOnLoad;
        }
        public T OnCreate()
        {
            var instance = UnityEngine.Object.Instantiate(Prefab);
            if (m_DontDestroyOnLoad)
            {
                // Make sure the instance is not destroyed when the scene is changed
                // Because its lifecycle is managed by the pool
                UnityEngine.Object.DontDestroyOnLoad(instance.gameObject);
            }
            return instance;
        }

        public virtual void OnGet(T instance)
        {
            if (!m_KeepInactive)
            {
                instance.gameObject.SetActive(true);
            }
        }

        public virtual void OnReturn(T instance)
        {
            if (instance == null || instance.gameObject == null) return;
            if (instance is IResettable resettable)
            {
                resettable.Reset();
            }
            instance.gameObject.SetActive(false);
        }

        public virtual void OnDestroy(T instance)
        {
            UnityEngine.Object.Destroy(instance.gameObject);
        }

        public object GetKey()
        {
            return (Prefab, GetType());
        }

        public static implicit operator ComponentPrefabPoolConfig<T>(T poolRule)
        {
            return new ComponentPrefabPoolConfig<T>(poolRule);
        }
    }
}