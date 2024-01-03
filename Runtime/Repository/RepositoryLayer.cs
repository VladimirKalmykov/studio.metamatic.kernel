using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.UI;

namespace Metamatic
{
    public interface IRepositoryDataLayer {
        public string name { get; }
        public IReadOnlyList<IRepositoryDataLayer> children { get; }
        public IRepositoryDataLayer  parent { get; }
    }
    public class RepositoryLayer: IDisposable
    {
        private RepositoryLayer m_Parent;
        public RepositoryLayer parent => m_Parent;
        private List<RepositoryLayer> m_Children = new();
        public IReadOnlyList<RepositoryLayer> children => m_Children;
        public string name { get; }
        /// <summary>
        /// ALl stores in current data layer
        /// </summary>
        private Dictionary<Type, Dictionary<string, RepositoryInternalStore>> m_Stores = new();
        /// <summary>
        /// Reference to all stores in current data layer (read only)
        /// </summary>
        public IReadOnlyDictionary<Type, Dictionary<string, RepositoryInternalStore>> stores => m_Stores;
        /// <summary>
        /// Reference to repository behaviour
        /// </summary>
        public readonly RepositoryBehaviour repositoryBehaviour;
        /// <summary>
        /// Listeners for store changes
        /// </summary>
        private readonly Dictionary<Type, Dictionary<string, List<Action>>> m_StoreListeners = new ();
        private event Action OnStoresChanged;
        private bool m_IsDisposed;

        public RepositoryLayer(RepositoryBehaviour repositoryBehaviour, string mName, RepositoryLayer parent = null)
        {
            this.repositoryBehaviour = repositoryBehaviour;
            this.name = mName;
            m_Parent = parent;
            if (m_Parent != null)
            {
                m_Parent.m_Children.Add(this);
            }
        }

        ~RepositoryLayer()
        {
            Dispose();
        }
        
        /// <summary>
        /// Get store for specific data type and name in current data layer
        /// </summary>
        /// <param name="name"></param>
        /// <param name="store"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public bool InternalTryGetStore<T>(string name, out RepositoryInternalStore<T> store, bool forceCreateIfNoExists = false)
        where T: struct
        {
            store = null;
            if (m_Stores.TryGetValue(typeof(T), out var typeStores))
            {
                if (typeStores.TryGetValue(name, out var internalStore))
                {
                    store = internalStore as RepositoryInternalStore<T>;
                    return true;
                }
            }

            if (forceCreateIfNoExists)
            {
                store = CreateStore<T>(name);
                return true;
            }
            return false;
        }

        private RepositoryInternalStore<T> CreateStore<T>(string name) where T: struct
        {
            var internalStore = new RepositoryInternalStore<T>();
            if (!m_Stores.TryGetValue(typeof(T), out var typeStores))
            {
                typeStores = new Dictionary<string, RepositoryInternalStore>();
                m_Stores.Add(typeof(T), typeStores);
            }
            typeStores.Add(name, internalStore);
            OnStoresChanged?.Invoke();
            EmitStoreChanged<T>(name);
            return internalStore;
        }
        
        private void EmitStoreChanged<T>(string name) where T: struct
        {
            if (m_StoreListeners.TryGetValue(typeof(T), out var typeListeners))
            {
                if (typeListeners.TryGetValue(name, out var listeners))
                {
                    foreach (var listener in listeners)
                    {
                        listener();
                    }
                }
            }
            m_Children.ForEach(child => child.EmitStoreChanged<T>(name));
        }
        
        /// <summary>
        /// Try get store for specific data type and name in parent data layer
        /// </summary>
        /// <param name="name"></param>
        /// <param name="store"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private bool InternalTryResolveStoreInParent<T>(string name, out RepositoryInternalStore<T> store)
        where T: struct
        {
            store = null;
            if (m_Parent != null)
            {
                return m_Parent.InternalTryResolveStore(name, out store);
            }
            return false;
        }
        
        /* Subscribe to current layer changes and try to resolve store every time data layer changed */
        internal Action InternalSubscribeStore<T>(string name, Action<RepositoryInternalStore<T>> callback)
        where T: struct
        {
            if (!m_StoreListeners.TryGetValue(typeof(T), out var typeListeners))
            {
                typeListeners = new Dictionary<string, List<Action>>();
                m_StoreListeners.Add(typeof(T), typeListeners);
            }
            if (!typeListeners.TryGetValue(name, out var listeners))
            {
                listeners = new List<Action>();
                typeListeners.Add(name, listeners);
            }
            var listener = new Action(() =>
            {
                if (InternalTryResolveStore<T>(name, out var store))
                {
                    callback(store);
                }
                else
                {
                    callback(null);
                }
            });
            listeners.Add(listener);
            listener();

            return () =>
            {
                listeners.Remove(listener);
                if (listeners.Count == 0)
                {
                    typeListeners.Remove(name);
                }
                if (typeListeners.Count == 0)
                {
                    m_StoreListeners.Remove(typeof(T));
                }
            };
        }
        
        /// <summary>
        /// Subscribe to stores changes (new store added or removed)
        /// </summary>
        /// <param name="callback"></param>
        /// <returns></returns>
        public Action SubscribeStoresChanged(Action callback)
        {
            OnStoresChanged += callback;
            return () =>
            {
                OnStoresChanged -= callback;
            };
        }
        
        /// <summary>
        /// Try get store for specific data type and name in current or parent data layer
        /// </summary>
        /// <param name="name"></param>
        /// <param name="store"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private bool InternalTryResolveStore<T>(string name, out RepositoryInternalStore<T> store)
        where T: struct
        {
            store = null;
            if (InternalTryGetStore(name, out store))
            {
                return true;
            }

            if (InternalTryResolveStoreInParent(name, out store))
            {
                return true;
            }
            return false;
        }

        public void Dispose()
        {
            if (m_IsDisposed)
            {
                return;
            }
            m_IsDisposed = true;
            /* Dispose all children layers */
            m_Children.ForEach(child => child.Dispose());
            m_Children.Clear();
            /* Dispose all stores */
            foreach (var typeStores in m_Stores)
            {
                foreach (var store in typeStores.Value)
                {
                    store.Value?.Dispose();
                }
            }
            m_Stores.Clear();
            m_StoreListeners.Clear();
            if (m_Parent != null)
            {
                m_Parent.m_Children.Remove(this);
            }
            OnStoresChanged?.Invoke();
            GC.SuppressFinalize(this);
        }

        #region Private methods
        
        private bool HasParent()
        {
            return m_Parent != null;
        }

        #endregion
    }
}