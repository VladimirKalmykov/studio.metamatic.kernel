using Metamatic.Storages;
using UnityEngine;

namespace Metamatic
{
    public static class Repository
    {
        private static IStorage m_LocalStorage;

        public static IStorage LocalStorage
        {
            get
            {
                if (m_LocalStorage == null)
                {
                    switch (Application.platform)
                    {
                        case RuntimePlatform.WindowsPlayer:
                        case RuntimePlatform.WindowsEditor:
                            case RuntimePlatform.Android:
                            m_LocalStorage = new FileSystemStorage();
                            break;
                        case RuntimePlatform.WebGLPlayer:
                            m_LocalStorage = new WebLocalStorage();
                            break;
                        default:
                            m_LocalStorage = new LocalMemoryStorage();
                            break;
                    }
                }

                return m_LocalStorage;
            }
        }
    }
}