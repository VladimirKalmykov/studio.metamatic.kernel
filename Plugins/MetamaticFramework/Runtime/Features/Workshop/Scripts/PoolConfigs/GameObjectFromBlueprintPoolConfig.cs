using Metamatic.Framework.Workshop;
using UnityEngine;

namespace Metamatic
{
    public readonly struct GameObjectFromBlueprintPoolConfig: IPoolConfig<GameObject>
    {
        private readonly GameObject m_Prefab;
        private readonly bool m_KeepInactive;
        private readonly bool m_DontDestroyOnLoad;
        private readonly IObjectBlueprint m_Blueprint;
        private readonly string m_Address;
        private readonly bool m_AutoRecyclable;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="prefab"></param>
        /// <param name="blueprint"></param>
        /// <param name="address"></param>
        /// <param name="keepInactive"></param>
        /// <param name="dontDestroyOnLoad"></param>
        /// <param name="autoRecyclable">Return object to the pool when called recycle. Call recycle on return to the pool.</param>
        public GameObjectFromBlueprintPoolConfig(
            GameObject prefab,
            IObjectBlueprint blueprint,
            string address,
            bool keepInactive = false,
            bool dontDestroyOnLoad = false,
            bool autoRecyclable = false
            )
        {
            m_Prefab = prefab;
            m_KeepInactive = keepInactive;
            m_DontDestroyOnLoad = dontDestroyOnLoad;
            m_Blueprint = blueprint;
            m_Address = address;
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
            var origin = instance.GetOrAddComponent<BlueprintOrigin>();
            origin.blueprint = m_Blueprint;
            origin.address = m_Address;
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