using System;
using UnityEngine;

namespace Metamatic
{
    public class DatabaseSelector
    {
        public delegate void BranchAndEntityHandler(string branchName, string entityName);
        public readonly DatabaseBranchSource branch;
        public readonly DatabaseEntitySource entity;
        
        /// <summary>
        /// Absolute address of the database entity
        /// </summary>
        public string address
        {
            get
            {
                return $"{branch.value}{Database.ADDRESS_ENTITY_SEP}{entity.value}";
            }
        }

        public DatabaseSelector(DatabaseBranchSource branchSource)
        {
            this.branch = new DatabaseBranchSource(branchSource);
            this.entity = new DatabaseEntitySource(Database.currentBranchSource);
        }
        
        public DatabaseSelector(DatabaseBranchSource branchSource, DatabaseEntitySource entitySource)
        {
            this.branch = new DatabaseBranchSource(branchSource);
            this.entity = new DatabaseEntitySource(entitySource);
        }

        public DatabaseSelector(DatabaseEntitySource entitySource)
        {
            this.entity = new DatabaseEntitySource(entitySource);
            this.branch = new DatabaseBranchSource(Database.currentBranchSource);
        }
        
        /// <summary>
        /// Get branch selector by name
        /// </summary>
        /// <param name="branchName"></param>
        /// <returns></returns>
        public DatabaseSelector Branch(string branchName)
        {
            this.branch.PlugIn(new DatabaseBranchSource(branchName));
            return this;
        }
        
        /// <summary>
        /// Get database branch selector by behaviour (it will resolve DatabaseGameObjectContext and get branch source from it)
        /// </summary>
        /// <param name="behaviour"></param>
        /// <returns></returns>
        public DatabaseSelector Branch(MonoBehaviour behaviour)
        {
            var gameObjectContext = behaviour.GetComponentInParent<DatabaseGameObjectContext>();
            if (gameObjectContext == null)
            {
                return new DatabaseSelector(new DatabaseBranchSource(Database.masterBranchName));
            }

            return new DatabaseSelector(new DatabaseBranchSource(gameObjectContext.branchSource));
        }
        
        /// <summary>
        /// Get database entity selector by name
        /// </summary>
        /// <param name="entityName"></param>
        /// <returns></returns>
        public DatabaseSelector Entity(string entityName)
        {
            this.entity.PlugIn(new DatabaseEntitySource(entityName));
            return this;
        }
        
        /// <summary>
        /// Get database entity selector by behaviour component
        /// </summary>
        /// <param name="behaviour"></param>
        /// <returns></returns>
        public DatabaseSelector Entity(MonoBehaviour behaviour)
        {
            var gameObjectEntity = behaviour.GetComponentInParent<DatabaseContextBehaviour>();
            if (gameObjectEntity == null)
            {
                return Entity(behaviour.gameObject.name);
            }
            this.entity.PlugIn(new DatabaseEntitySource(gameObjectEntity.entitySource));
            return this;
        }
        
        /// <summary>
        /// Subscribe to any changes of entity or branch name
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        public Action Subscribe(BranchAndEntityHandler handler)
        {
            var cachedBranchName = branch.value;
            var cachedEntityName = entity.value;
            void OnBranchOrEntityChange()
            {
                cachedBranchName = branch.value;
                cachedEntityName = entity.value;
                handler(cachedBranchName, cachedEntityName);
            }

            var unsubscriveBranch = branch.Subscribe(branchName =>
            {
                if (cachedBranchName == branchName) return;
                OnBranchOrEntityChange();
            });
            var unsubscribeEntity = branch.Subscribe(entityName =>
            {
                if (cachedEntityName == entityName) return;
                OnBranchOrEntityChange();
            });

            return () =>
            {
                unsubscriveBranch();
                unsubscribeEntity();
            };
        }
        
        /// <summary>
        /// Get store attached to the database selection
        /// </summary>
        /// <typeparam name="TState"></typeparam>
        /// <returns></returns>
        public DatabaseVirtualStore<TState> GetStore<TState>()
        {
            return new DatabaseVirtualStore<TState>(this);
        }
        
        /// <summary>
        /// Get store attached to the database selection
        /// </summary>
        /// <typeparam name="TState"></typeparam> 
        /// <returns></returns>
        public DatabaseVirtualStore<TState> GetStore<TState>(TState initialState)
        {
            return new DatabaseVirtualStore<TState>(this, initialState);
        }

        /// <summary>
        /// Get virtual store with specified entity name
        /// </summary>
        /// <param name="entityName">Name of the entity</param>
        /// <param name="initialState">Initial (Fallback) state</param>
        /// <typeparam name="TState"></typeparam>
        /// <returns></returns>
        public DatabaseVirtualStore<TState> GetStore<TState>(string entityName, TState initialState = default)
        {
            return new DatabaseVirtualStore<TState>(new DatabaseSelector(branch, new DatabaseEntitySource(entityName)), initialState);
        }
    }
}