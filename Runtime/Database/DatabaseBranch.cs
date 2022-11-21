using System;
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

        public delegate void EntityHandler(DatabaseEntity entity);
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
            Debug.Log($"Create new database entity {name}");
            entity = new DatabaseEntity(name);
            m_Entities[name] = entity;
            OnEntityOperation?.Invoke(name, entity, EntityOperation.CREATE);
            return entity;
        }
        
        /// <summary>
        /// Subscribe to entity by name
        /// </summary>
        /// <param name="entityName"></param>
        /// <param name="handler"></param>
        /// <returns></returns>
        public Action SubscribeEntity(string entityName, EntityHandler handler)
        {
            DatabaseEntity entity = null;
            m_Entities.TryGetValue(entityName, out entity);
            EntityOperationHandler entityOperationHandler = (name, entity, operation) =>
            {
                if (name == entityName)
                {
                    handler(entity);
                }
            };
            OnEntityOperation += entityOperationHandler;
            handler(entity);

            return () =>
            {
                OnEntityOperation -= entityOperationHandler;
            };
        }
    }
}