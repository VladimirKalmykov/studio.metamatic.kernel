using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

namespace Metamatic
{
    public class RepositoryBehaviour: MonoBehaviour
    {
        public delegate void BeforeDestroy();
        public event BeforeDestroy OnBeforeDestroy;

        public RepositoryStorageType storageType { get; private set; }
        private Dictionary<(ObservableValue, Type, string), RepositoryStore> m_VirtualStoresPool = new();
        private RepositoryDataLayer m_RootLayer;
        public RepositoryDataLayer rootLayer
        {
            get
            {
                if (m_RootLayer == null)
                {
                    m_RootLayer = new RepositoryDataLayer(this);
                }
                return m_RootLayer;
            }
        }
        public ObservableWritableValue<RepositoryDataLayer> currentLayer { get; private set; }
        public void Initialize(RepositoryStorageType type = RepositoryStorageType.FileSystem)
        {
            storageType = type;
            currentLayer.SetValue(rootLayer);
        }
        
        #region Public methods

        public RepositoryStore<T> GetStore<T>(string name, T defaultValue = default) where T : struct
        {
            if (!m_VirtualStoresPool.TryGetValue((currentLayer, typeof(T), name), out var store))
            {
                store = new RepositoryStore<T>(this, currentLayer, name);
                store.OnDispose += () =>
                {
                    m_VirtualStoresPool.Remove((currentLayer, typeof(T), name));
                };
            }
            store.Acquire();
            return store as RepositoryStore<T>;
        }
        
        public RepositoryAsyncOperation PullStore<T>(string name, CancellationToken cancellationToken)
        where T: struct
        {
            var operation = new RepositoryAsyncOperation(out var setProgress, out var setError);
            // Здесь асинхронная логика загрузки данных из хранилища
            setProgress(1);
            return operation;
        }
        
        #endregion
        
        #region Mono Behaviour lifecycle methods

        private void OnDestroy()
        {
            OnBeforeDestroy?.Invoke();
        }

        #endregion
    }
}