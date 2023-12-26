using System;
using UnityEngine;

namespace Metamatic
{
    public class DatabaseStoreProvider<T>: MonoBehaviour
        where T : struct
    {
        [SerializeField] private string entity = "void";

        private DatabaseVirtualStore<T> m_Store;
        public DatabaseVirtualStore<T> store => m_Store ??= Database.GetStore(entity, GetDefaultValue());

        private void OnDisable()
        {
            m_Store.Dispose();
        }

        protected virtual T GetDefaultValue()
        {
            return default;
        }
    }
}