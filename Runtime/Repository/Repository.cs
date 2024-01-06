using UnityEngine;

namespace Metamatic
{
    
    public class Repository
    {
        private static RepositoryBehaviour m_Instance;
        public static RepositoryBehaviour instance
        {
            get
            {
                if (m_Instance == null)
                {
                    #if UNITY_EDITOR
                    /* Find repository in scene */
                    m_Instance = Object.FindObjectOfType<RepositoryBehaviour>();
                    if (m_Instance != null)
                    {
                        m_Instance.Initialize(RepositoryStorageType.FileSystem);
                        return m_Instance;
                    }
                    #else
                    m_Instance = new GameObject("Repository").AddComponent<RepositoryBehaviour>();
                    m_Instance.Initialize(RepositoryStorageType.FileSystem);
                    #endif
                }
                return m_Instance;
            }
        }
        /// <summary>
        /// Get store for specific data type and name
        /// </summary>
        /// <param name="name"></param>
        /// <param name="initialize">Initialize store in memory with writing default data</param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static RepositoryStore<T> GetStore<T>(string name) where T: struct
        {
            return instance.GetStore<T>(name);
        }
        
        /// <summary>
        /// Select layer by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static RepositoryLayerSelector SelectLayer(string name)
        {
            return instance.SelectLayer(name);
        }
    }
}