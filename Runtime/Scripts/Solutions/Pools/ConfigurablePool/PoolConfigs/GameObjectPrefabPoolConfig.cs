using UnityEngine;

namespace Metamatic
{
    public readonly struct GameObjectConfigurablePoolConfig: IPoolConfig<GameObject>
    {
        private readonly GameObject m_Prefab;
        private readonly bool m_KeepInactive;
        private readonly bool m_DontDestroyOnLoad;
        private readonly bool m_AutoRecyclable;
        public GameObjectConfigurablePoolConfig(GameObject prefab, bool keepInactive = false, bool dontDestroyOnLoad = false, bool autoRecyclable = true)
        {
            m_Prefab = prefab;
            m_KeepInactive = keepInactive;
            m_DontDestroyOnLoad = dontDestroyOnLoad;
            m_AutoRecyclable = autoRecyclable;
        }
        public object GetKey()
        {
            return (m_Prefab, GetType());
        }

        public GameObject OnCreate()
        {
            m_Prefab.SetActive(!m_KeepInactive);
            var instance = UnityEngine.Object.Instantiate(m_Prefab);
            if (m_DontDestroyOnLoad)
            {
                // Make sure the instance is not destroyed when the scene is changed
                // Because its lifecycle is managed by the pool
                UnityEngine.Object.DontDestroyOnLoad(instance.gameObject);
            }
            instance.SetActive(!m_KeepInactive);
            return instance;
        }

        public void OnGet(GameObject instance)
        {
            instance.SetActive(!m_KeepInactive);
        }

        public void OnReturn(GameObject instance)
        {
            instance.SetActive(false);
            if (m_AutoRecyclable)
            {
                instance.GetOrAddComponent<RecyclableGameObject>().Recycle();
            }
        }

        public void OnDestroy(GameObject instance)
        {
            Object.Destroy(instance);
        }
    }
}