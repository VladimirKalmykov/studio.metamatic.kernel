using UnityEngine;

namespace Metamatic
{
    public readonly struct GameObjectConfigurablePoolConfig: IPoolConfig<GameObject>
    {
        private readonly GameObject m_Prefab;
        public GameObjectConfigurablePoolConfig(GameObject prefab)
        {
            m_Prefab = prefab;
        }
        public object GetKey()
        {
            return (m_Prefab, GetType());
        }

        public GameObject OnCreate()
        {
            return UnityEngine.Object.Instantiate(m_Prefab);
        }

        public void OnGet(GameObject instance)
        {
            instance.SetActive(true);
        }

        public void OnReturn(GameObject instance)
        {
            instance.SetActive(false);
        }

        public void OnDestroy(GameObject instance)
        {
            Object.Destroy(instance);
        }
    }
}