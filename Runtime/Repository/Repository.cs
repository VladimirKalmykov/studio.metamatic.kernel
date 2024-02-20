using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Metamatic;
using UnityEngine;

namespace Metamatic
{
    public class Repository: AsyncBehaviour
    {
        private static Repository m_Instance;
        public static Repository Global
        {
            get
            {
                if (m_Instance == null)
                {
                    
                    m_Instance = new GameObject("Repository").AddComponent<Repository>();
                    m_Instance.Initialize();
#if UNITY_EDITOR
                    m_Instance.gameObject.hideFlags = HideFlags.DontSaveInEditor & HideFlags.DontSave;
#endif
                }
                return m_Instance;
            }
        }

        [SerializeField] private bool isGlobal;
        
        private Dictionary<(Type type, string uri), InternalStore> m_Stores = new Dictionary<(Type type, string uri), InternalStore>();
        
        public readonly ObservableWritableValue<IOSource> m_Source = new ObservableWritableValue<IOSource>(new FsJsonSource());

        public IOSource source
        {
            get => m_Source.value;
            private set => m_Source.value = value;
        }
        
        public AsyncReadOperation<T> Read<T>(string address, IOSource source, CancellationToken token = default)
        {
            return source.Read<T>(address, token);
        }
        
        public AsyncWriteOperation<T> Write<T>(string address, T value, IOSource source, CancellationToken token = default)
        {
            return source.Write<T>(address, value, token);
        }

        private void Initialize()
        {
            source = new FsJsonSource();
        }

        private void Awake()
        {
            if (m_Instance == null && isGlobal)
            {
                m_Instance = this;
                Initialize();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        protected override void RunAsync(CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }
        
        /// <summary>
        /// Get data address by its type and uri
        /// </summary>
        /// <param name="uri"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static string GetUriAddress<T>(string uri)
        {
            var typeName = typeof(T).FullName;
            return $"{typeName}/{uri}";
        }
        
        /// <summary>
        /// Initialize and get store from the repository
        /// </summary>
        /// <param name="uri"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public Store<T> GetStore<T>(string uri, T defaultValue = default, bool autoPush = false) where T: struct
        {
            if (!Global.m_Stores.TryGetValue((typeof(T), uri), out var internalStore))
            {
                internalStore = new InternalStore<T>(repository: this, uri: uri, defaultValue: defaultValue, autoPush: autoPush);
            }
            else
            {
                // TODO: Apply default value and auto push settings to the existing store
            }
            return new Store<T>(internalStore as InternalStore<T>);
        }
        
        /// <summary>
        /// Remove store from the repository
        /// </summary>
        /// <param name="store"></param>
        public void Remove(InternalStore store)
        {
            m_Stores.Remove((store.type, store.uri));
        }
        
        public void AssertError(Exception e)
        {
            Debug.LogException(e);
        }
    }
}