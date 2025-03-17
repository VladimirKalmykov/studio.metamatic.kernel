using UnityEngine;

namespace Metamatic.Framework.Workshop
{
    public class EntityPoolConfig: IPoolConfig<Entity>
    {
        public readonly Entity Prefab;
        public EntityPoolConfig(Entity prefab)
        {
            Prefab = prefab;
        }
        public object GetKey()
        {
            return (Prefab, GetType());
        }

        public Entity OnCreate()
        {
            return UnityEngine.Object.Instantiate(Prefab);
        }

        public void OnGet(Entity instance)
        {
            instance.gameObject.SetActive(true);
        }

        public void OnReturn(Entity instance)
        {
            instance.ResetEntity();
            instance.gameObject.SetActive(false);
        }

        public void OnDestroy(Entity instance)
        {
            Object.Destroy(instance);
        }
    }
}