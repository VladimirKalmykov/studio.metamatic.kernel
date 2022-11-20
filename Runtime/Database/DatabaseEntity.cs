using System;
using System.Collections.Generic;

namespace Metamatic
{
    public class DatabaseEntity
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

        public DatabaseEntity(string name)
        {
            this.name = name;
        }

        public DatabaseStore<TState> TouchStore<TState>()
        {
            var type = typeof(TState);
            DatabaseStore store = null;
            if (m_Stores.TryGetValue(type, out store))
            {
                return (DatabaseStore<TState>) store;
            }

            store = new DatabaseStore<TState>();
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