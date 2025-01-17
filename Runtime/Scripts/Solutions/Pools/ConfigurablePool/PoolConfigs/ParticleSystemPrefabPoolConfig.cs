using UnityEngine;

namespace Metamatic
{
    public class ParticleSystemPrefabPoolConfig: IPoolConfig<ParticleSystem>
    {
        public readonly ParticleSystem Prefab;
        public ParticleSystemPrefabPoolConfig(ParticleSystem prefab)
        {
            Prefab = prefab;
        }
        public ParticleSystem OnCreate()
        {
            return UnityEngine.Object.Instantiate(Prefab);
        }

        public virtual void OnGet(ParticleSystem instance)
        {
            instance.gameObject.SetActive(true);
        }

        public virtual void OnReturn(ParticleSystem instance)
        {
            instance.Stop();
            instance.gameObject.SetActive(false);
        }

        public virtual void OnDestroy(ParticleSystem instance)
        {
            UnityEngine.Object.Destroy(instance.gameObject);
        }

        public object GetKey()
        {
            return (Prefab, GetType());
        }
    }
}