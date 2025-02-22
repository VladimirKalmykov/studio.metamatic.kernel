using Metamatic.Storages;
using UnityEngine;

namespace Metamatic
{
    public static class Repository
    {
        private static IStorage m_LocalStorageLegacy;
        private static IStorage m_LocalStorage;

        public static IStorage localStorageLegacy
        {
            get
            {
                if (m_LocalStorageLegacy == null)
                {
                    switch (Application.platform)
                    {
                        case RuntimePlatform.WindowsPlayer:
                        case RuntimePlatform.WindowsEditor:
                            case RuntimePlatform.Android:
                            m_LocalStorageLegacy = new FileSystemStorage(null, IStorage.TypeVersionControl.LegacyMetadataContainer);
                            break;
                        case RuntimePlatform.WebGLPlayer:
                            m_LocalStorageLegacy = new WebLocalStorage(IStorage.TypeVersionControl.LegacyMetadataContainer);
                            break;
                        default:
                            m_LocalStorageLegacy = new LocalMemoryStorage(IStorage.TypeVersionControl.LegacyMetadataContainer);
                            break;
                    }
                }

                return m_LocalStorageLegacy;
            }
        }
        
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
                            m_LocalStorage = new FileSystemStorage(null, IStorage.TypeVersionControl.Orchard);
                            break;
                        case RuntimePlatform.WebGLPlayer:
                            m_LocalStorage = new WebLocalStorage(IStorage.TypeVersionControl.Orchard);
                            break;
                        default:
                            m_LocalStorage = new LocalMemoryStorage(IStorage.TypeVersionControl.Orchard);
                            break;
                    }
                }

                return m_LocalStorage;
            }
        }
    }
}