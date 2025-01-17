namespace Metamatic
{
    public class ComponentPrefabPoolConfig<T>: IPoolConfig<T>
        where T: UnityEngine.Component
    {
        public readonly T Prefab;
        public ComponentPrefabPoolConfig(T prefab)
        {
            Prefab = prefab;
        }
        public T OnCreate()
        {
            return UnityEngine.Object.Instantiate(Prefab);
        }

        public virtual void OnGet(T instance)
        {
            instance.gameObject.SetActive(true);
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