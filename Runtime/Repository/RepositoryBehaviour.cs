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
        private RepositoryLayer m_RootLayer;
        private List<RepositoryLayer> m_Layers = new();
        public IReadOnlyList<RepositoryLayer> layers => m_Layers;
        
        #region Events
        public event Action<RepositoryLayer> OnLayerAdded;
        public event Action<RepositoryLayer> OnLayerRemoved;
        public event Action<RepositoryLayer, RepositoryLayer> OnLayerMerged;
        #endregion

        public RepositoryLayer rootLayer => m_RootLayer;

        private RepositoryObservableWritableLayer m_CurrentLayer;
        public RepositoryObservableWritableLayer currentLayer
        {
            get
            {
                if (m_CurrentLayer == null)
                {
                    m_CurrentLayer = new RepositoryObservableWritableLayer(this, "Master");
                }
                return m_CurrentLayer;
            }
        }
        public void Initialize(RepositoryStorageType type = RepositoryStorageType.FileSystem)
        {
            storageType = type;
            InternalCreateLayer("Master", null);
        }
        
        #region Public methods
        
        /// <summary>
        /// Get store for specific data type and name for the current layer
        /// </summary>
        /// <param name="name"></param>
        /// <param name="defaultValue"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
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
        
        /// <summary>
        /// Get store for specific data type and name for the specific layer
        /// </summary>
        /// <param name="observableLayer"></param>
        /// <param name="name"></param>
        /// <param name="defaultValue"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public RepositoryStore<T> GetStore<T>(RepositoryObservableLayer observableLayer, string name, T defaultValue = default) where T : struct
        {
            if (!m_VirtualStoresPool.TryGetValue((observableLayer, typeof(T), name), out var store))
            {
                store = new RepositoryStore<T>(this, observableLayer, name);
                m_VirtualStoresPool.Add((observableLayer, typeof(T), name), store);
                store.OnDispose += () =>
                {
                    m_VirtualStoresPool.Remove((observableLayer, typeof(T), name));
                };
            }
            store.Acquire();
            return store as RepositoryStore<T>;
        }

        public RepositoryStore<T> GetStore<T>(string layerName, string entityName, T defaultValue = default)
            where T : struct
        {
            var repositoryObservableLayer = RepositoryObservableLayer.Get(this, layerName);
            return GetStore(repositoryObservableLayer, entityName, defaultValue);
        }
        
        /// <summary>
        /// Select layer by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public RepositoryLayerSelector SelectLayer(string name)
        {
            return new RepositoryLayerSelector(this, name);
        }
        
        /// <summary>
        /// Create new layer
        /// </summary>
        /// <param name="name"></param>
        /// <param name="parent"></param>
        /// <returns></returns>
        private RepositoryLayer InternalCreateLayer(string name, RepositoryLayer parent = null)
        {
            #if DEBUG
            /* Check name already exists */
            foreach (var l in m_Layers)
            {
                if (l.name == name)
                {
                    throw new Exception($"Layer with name {name} already exists");
                }
            }
            #endif
            var layer = new RepositoryLayer(this, name, parent);
            m_Layers.Add(layer);
            OnLayerAdded?.Invoke(layer);
            return layer;
        }
        
        /// <summary>
        /// Create and select layer
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public RepositoryObservableLayer CreateLayer(string name, RepositoryLayer parent = null)
        {
            InternalCreateLayer(name, parent);
            return new RepositoryObservableLayer(this, name);
        }
        
        /// <summary>
        /// Create or select layer for the current layer and set it as current
        /// </summary>
        /// <returns></returns>
        public RepositoryLayer Checkout(string name)
        {
            RepositoryLayer layer = null;
            /* Check for existing layer with the same name */
            foreach (var l in m_Layers)
            {
                if (l.name == name)
                {
                    layer = l;
                }
            }
            if (layer == null)
            {
                layer = InternalCreateLayer(name, currentLayer.value);
            }
            return layer;
        }
        
        /// <summary>
        /// Remove current layer and set parent layer as current
        /// </summary>
        /// <exception cref="Exception"></exception>
        public void ReleaseCurrentLayer()
        {
            var current = currentLayer.value;
            if (current != rootLayer)
            {
                current.Dispose();
                Debug.Assert(current.parent != null, "Removed layer must have parent");
                currentLayer.SetValue(current.parent);
            }
            else
            {
                throw new Exception("Cannot remove root layer");
            }
        }
        
        /// <summary>
        /// Load store from storage
        /// </summary>
        /// <param name="name"></param>
        /// <param name="cancellationToken"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
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