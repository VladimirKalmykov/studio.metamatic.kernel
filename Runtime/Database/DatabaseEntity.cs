using System;
using System.Collections.Generic;
using UnityEngine;

namespace Metamatic
{
    public class DatabaseEntity: IDisposable
    {
        public enum StoreOperation
        {
            CREATE,
            REMOVE
        }

        public delegate void StoreHandler<TState>(DatabaseStore<TState> store);
        public delegate void StoreOperationHandler(Type type, DatabaseStore store, StoreOperation operation);
        public readonly string name;
        public event StoreOperationHandler OnStoreOperation;
        private Dictionary<Type, DatabaseStore> m_Stores = new Dictionary<Type, DatabaseStore>();
        public readonly DatabaseBranch branch;
        
        /// <summary>
        /// Absolute address of the database entity
        /// </summary>
        public string address
        {
            get
            {
                return $"{branch.address}:${this.name}";
            }
        }

        public DatabaseEntity(DatabaseBranch branch, string name)
        {
            this.name = name;
            this.branch = branch;
        }
        
        public void Dispose()
        {
            foreach (var pair in m_Stores)
            {
                pair.Value.Dispose();
            }
        }

        public DatabaseStore<TState> TouchStore<TState>(ref TState initialState, DatabaseStore<TState>.StateModifier modifier = null)
        {
            var type = typeof(TState);
            DatabaseStore store = null;
            if (m_Stores.TryGetValue(type, out store))
            {
                return (DatabaseStore<TState>) store;
            }

            Debug.Log($"Create new database store {typeof(TState)}");
            store = new DatabaseStore<TState>(this, initialState);
            if (modifier != null)
            {
                ((DatabaseStore<TState>)store).SetState(modifier, true);
            }
            m_Stores[type] = store;
            OnStoreOperation?.Invoke(type, store, StoreOperation.CREATE);
            return (DatabaseStore<TState>)store;
        }
        
        /// <summary>
        /// Subscribe to the store object by name
        /// </summary>
        /// <param name="handler"></param>
        /// <typeparam name="TState"></typeparam>
        /// <returns></returns>
        public Action SubscribeStore<TState>(StoreHandler<TState> handler)
        {
            var storeType = typeof(TState);
            DatabaseStore store = null;
            m_Stores.TryGetValue(storeType, out store);
            StoreOperationHandler entityOperationHandler = (type, store, operation) =>
            {
                if (type == storeType)
                {
                    handler((DatabaseStore<TState>)store);
                }
            };
            OnStoreOperation += entityOperationHandler;
            handler((DatabaseStore<TState>)store);

            return () =>
            {
                OnStoreOperation -= entityOperationHandler;
            };
        }
    }
}