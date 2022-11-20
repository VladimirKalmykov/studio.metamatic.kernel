using System.Collections.Generic;
using UnityEngine;

namespace Metamatic
{
    public class DatabaseBranch
    {
        public enum EntityOperation
        {
            CREATE,
            REMOVE
        }
        public delegate void EntityOperationHandler(string name, DatabaseEntity entity, EntityOperation operation);
        public readonly string name;
        public static Dictionary<string, DatabaseEntity> m_Entities = new Dictionary<string, DatabaseEntity>();
        public event EntityOperationHandler OnEntityOperation;

        public DatabaseBranch(string name)
        {
            this.name = name;
        }
        
        /// <summary>
        /// Get or create entity
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public DatabaseEntity TouchEntity(string name)
        {
            DatabaseEntity entity = null;
            if (m_Entities.TryGetValue(name, out entity))
            {
                return entity;
            }
            entity = new DatabaseEntity(name);
            m_Entities[name] = entity;
            OnEntityOperation?.Invoke(name, entity, EntityOperation.CREATE);
            return entity;
        }
    }
}