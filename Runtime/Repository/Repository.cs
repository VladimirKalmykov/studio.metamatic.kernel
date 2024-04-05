using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Metamatic
{
    public class Repository: AsyncBehaviour
    {
        public static readonly string SettingsPath = "Assets/Metamatic/Resources/Settings/Repository.asset";
        private static Repository m_Instance;
        public static Repository Global
        {
            get
            {
                if (m_Instance == null)
                {
                    m_Instance = new GameObject("__GLOBALREPOSITORY__").AddComponent<Repository>();
                    m_Instance.isGlobal = true;
#if UNITY_EDITOR
                    m_Instance.gameObject.hideFlags = HideFlags.DontSaveInEditor & HideFlags.DontSave;
#endif
                    m_Instance.InitializeGlobal();
                }
                return m_Instance;
            }
        }

        [SerializeField] private bool isGlobal;
        
        public readonly MicroState<IOSource> m_Source = new MicroState<IOSource>(null);

        public IOSource source
        {
            get => m_Source.value;
            private set => m_Source.value = value;
        }
        
        public AsyncReadOperation<T> Read<T>(string address, CancellationToken token = default)
        {
            return source.Read<T>(address, token);
        }
        
        public AsyncWriteOperation<T> Write<T>(string address, T value, CancellationToken token = default)
        {
            return source.Write<T>(address, value, token);
        }

        private void InitializeGlobal()
        {
            /* Load settings */
            var settings = Resources.Load<RepositorySettings>("Settings/Repository");
            var platforms = settings != null ? settings.platforms : new PlatformSource[] { };
            foreach (var platform in platforms)
            {
                if (platform.platform == Application.platform)
                {
                    var sourceType = Type.GetType(platform.sourceClassName);
                    if (sourceType != null)
                    {
                        source = (IOSource) Activator.CreateInstance(sourceType);
                        // source.Initialize(platform.settings);
                        break;
                    }
                    else
                    {
                        Debug.LogError("Source class not found");
                    }
                }
            }
            if (source == null)
                source = new LocalMemorySource();
            
            /* Display source class name */
            Debug.Log($"<color=red>Repository source</color>: {source.GetType().Name}");
        }

        protected override void RunAsync(CancellationToken cancellationToken)
        {
            
        }
        
        /// <summary>
        /// Get data address by its type and uri
        /// </summary>
        /// <param name="uri"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        [Obsolete("Use GetUriAdress in IOSource instead")]
        public static string GetUriAddress<T>(string uri)
        {
            var typeName = typeof(T).FullName;
            return $"{typeName}/{uri}";
        }
        
        public void AssertError(Exception e)
        {
            Debug.LogException(e);
        }
    }
}