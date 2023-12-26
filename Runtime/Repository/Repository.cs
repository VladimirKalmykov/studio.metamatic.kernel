using UnityEngine;

namespace Metamatic
{
    interface IRepository
    {
        public RepositoryStore<T> GetStore<T>(string name, T defaultValue = default) where T : struct;
    }
    public class Repository: IRepository
    {
        private static RepositoryBehaviour m_Instance;
        public static RepositoryBehaviour instance
        {
            get
            {
                if (m_Instance == null)
                {
                    m_Instance = new GameObject("Repository").AddComponent<RepositoryBehaviour>();
                    m_Instance.Initialize(RepositoryStorageType.FileSystem);
                }
                return m_Instance;
            }
        }
        /// <summary>
        /// Get store for specific data type and name
        /// </summary>
        /// <param name="name"></param>
        /// <param name="defaultValue"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public RepositoryStore<T> GetStore<T>(string name, T defaultValue = default) where T: struct
        {
            return instance.GetStore(name, defaultValue);
        }
    }
}