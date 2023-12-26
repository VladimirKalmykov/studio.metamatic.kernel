using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.UI;

namespace Metamatic
{
    public class RepositoryDataLayer
    {
        private RepositoryDataLayer m_Parent;
        private List<RepositoryDataLayer> m_Children = new();
        private Dictionary<Type, Dictionary<string, RepositoryInternalStore>> m_Stores = new();
        public readonly RepositoryBehaviour repositoryBehaviour;
        private readonly Dictionary<Type, Dictionary<string, List<Action>>> m_StoreListeners = new ();

        public RepositoryDataLayer(RepositoryBehaviour repositoryBehaviour)
        {
            this.repositoryBehaviour = repositoryBehaviour;
        }
        
        /// <summary>
        /// Get store for specific data type and name in current data layer
        /// </summary>
        /// <param name="name"></param>
        /// <param name="store"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public bool TryGetStore<T>(string name, out RepositoryInternalStore<T> store, bool forceCreateIfNoExists = false)
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
        public bool TryResolveStoreInParent<T>(string name, out RepositoryInternalStore<T> store)
        where T: struct
        {
            store = null;
            if (m_Parent != null)
            {
                return m_Parent.TryResolveStore(name, out store);
            }
            return false;
        }
        
        /* Subscribe to current layer changes and try to resolve store every time data layer changed */
        public Action SubscribeStore<T>(string name, Action<RepositoryInternalStore<T>> callback)
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
                if (TryResolveStore<T>(name, out var store))
                {
                    callback(store);
                }
                else
                {
                    callback(null);
                }
            });

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
        /// Try get store for specific data type and name in current or parent data layer
        /// </summary>
        /// <param name="name"></param>
        /// <param name="store"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public bool TryResolveStore<T>(string name, out RepositoryInternalStore<T> store)
        where T: struct
        {
            store = null;
            if (TryGetStore(name, out store))
            {
                return true;
            }

            if (TryResolveStoreInParent(name, out store))
            {
                return true;
            }
            return false;
        }

        #region Private methods

        private bool HasParent()
        {
            return m_Parent != null;
        }

        #endregion
    }
}