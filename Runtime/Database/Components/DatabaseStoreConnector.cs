using System;
using System.Collections.Generic;
using UnityEngine;

namespace Metamatic
{
    [Obsolete("Is obsolete, don't use it anymore", false)]
    public class DatabaseStoreConnector : DatabaseContextBehaviour
    {
        private List<IDisposable> m_VirtualStores = new List<IDisposable>();
        public DatabaseVirtualStore<T> GetStore<T>(T defaultValue = default) where T : struct
        {
            var store = Database.Use(this).GetStore<T>(defaultValue);
            m_VirtualStores.Add(store);
            store.OnDispose += () =>
            {
                m_VirtualStores.Remove(store);
            };
            return store;
        }

        private void OnDestroy()
        {
            foreach (var store in m_VirtualStores.ToArray())
            {
                store.Dispose();
            }
        }
    }
}