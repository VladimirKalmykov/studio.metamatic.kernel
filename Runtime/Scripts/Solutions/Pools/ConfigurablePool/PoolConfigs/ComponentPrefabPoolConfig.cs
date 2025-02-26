namespace Metamatic
{
    public class ComponentPrefabPoolConfig<T>: IPoolConfig<T>
        where T: UnityEngine.Component
    {
        public readonly T Prefab;
        private readonly bool m_KeepInactive;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="prefab"></param>
        /// <param name="keepInactive">Do not activate object on get from pool</param>
        public ComponentPrefabPoolConfig(T prefab, bool keepInactive = false)
        {
            Prefab = prefab;
            m_KeepInactive = keepInactive;
        }
        public T OnCreate()
        {
            return UnityEngine.Object.Instantiate(Prefab);
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