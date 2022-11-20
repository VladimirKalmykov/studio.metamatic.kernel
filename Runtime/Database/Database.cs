using System;
using System.Collections.Generic;
using UnityEngine;

namespace Metamatic
{
    
    public class Database
    {
        public enum BranchOperation
        {
            CREATE,
            REMOVE
        }
        public delegate void StoreHandler<TState>(DatabaseStore<TState> store);

        public delegate void BranchHandler(string name, DatabaseBranch branch, BranchOperation operation);
        public static readonly string masterBranchName = "master";
        public static event BranchHandler OnBranchOperation;
        public readonly Dictionary<string, DatabaseBranch> m_Branches = new Dictionary<string, DatabaseBranch>();
        
        /// <summary>
        /// Get store
        /// </summary>
        /// <param name="branchName"></param>
        /// <param name="entityName"></param>
        /// <typeparam name="TState"></typeparam>
        /// <returns></returns>
        public static DatabaseVirtualStore<TState> GetStore<TState>(string entityName, string branchName = null)
        {
            var branchSource = new DatabaseBranchSource(branchName ?? masterBranchName);
            var entitySource = new DatabaseEntitySource(entityName);
            var selector = new DatabaseSelector(branchSource, entitySource);
            return new DatabaseVirtualStore<TState>(selector);
        }

        public static DatabaseBranchSource currentBranchSource = new DatabaseBranchSource(masterBranchName);
        
        /// <summary>
        /// Get branch selector by name
        /// </summary>
        /// <param name="branchName"></param>
        /// <returns></returns>
        public static DatabaseSelector Branch(string branchName)
        {
            return new DatabaseSelector(new DatabaseBranchSource(branchName));
        }
        
        /// <summary>
        /// Get database branch selector by behaviour (it will resolve DatabaseGameObjectContext and get branch source from it)
        /// </summary>
        /// <param name="behaviour"></param>
        /// <returns></returns>
        public static DatabaseSelector Branch(MonoBehaviour behaviour)
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
        public static DatabaseSelector Entity(string entityName)
        {
            return new DatabaseSelector(new DatabaseEntitySource(entityName));
        }

        public static DatabaseSelector Entity(MonoBehaviour behaviour)
        {
            var gameObjectEntity = behaviour.GetComponentInParent<DatabaseGameObjectEntity>();
            if (gameObjectEntity == null)
            {
                return new DatabaseSelector(new DatabaseEntitySource(behaviour.gameObject.name));
            }
            return new DatabaseSelector(new DatabaseEntitySource(gameObjectEntity.entitySource));
        }
        
        /// <summary>
        /// Switch active branch to the specified
        /// </summary>
        /// <param name="name"></param>
        public static void CheckoutBranch(string name)
        {
            currentBranchSource.SetValue(name);
        }

        public Action SubscribeStore<TState>(string branchName, string entityName, StoreHandler<TState> handler)
        {
            // DatabaseEntity entity = null;
            // var currentBranchName = branchName;
            // var entityFullname = $"{branchName}:${entityName}";
            // while (m_Entities.TryGetValue(entityFullname, out entity))
            // {
            //     
            // }
            // var fallbacks = new List<string>();
            // var fallbackPath = branchName;
            // fallbacks.Add();
            // var lastSlash = -1;
            // while ((lastSlash = fallbackPath.LastIndexOf("/")) > 0)
            // {
            //     fallbackPath = fallbackPath.Substring(0, lastSlash);
            //     fallbacks.Add(fallbackPath);
            // }
        }

        public DatabaseBranch TouchBranch(string branchName)
        {
            DatabaseBranch branch = null;
            if (m_Branches.TryGetValue(branchName, out branch))
            {
                return branch;
            }
            branch = new DatabaseBranch(branchName);
            m_Branches[branchName] = branch;
            OnBranchOperation?.Invoke(branchName, branch, BranchOperation.CREATE);
            return branch;
        }
    }
}