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
    }
}