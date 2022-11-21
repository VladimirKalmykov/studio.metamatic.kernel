using System;
using System.Collections.Generic;
using UnityEngine;

namespace Metamatic
{
    
    public class Database: MonoBehaviour
    {
        public static Database instance { get; private set; }
        public enum BranchOperation
        {
            CREATE,
            REMOVE
        }
        
        public delegate void StoreHandler<TState>(DatabaseStore<TState> store);
        public delegate void BranchHandler(DatabaseBranch branch);
        public delegate void BranchOperationHandler(string name, DatabaseBranch branch, BranchOperation operation);
        public static readonly string masterBranchName = "master";
        public static event BranchOperationHandler OnBranchOperation;
        public static readonly Dictionary<string, DatabaseBranch> m_Branches = new Dictionary<string, DatabaseBranch>();
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            if (instance == null)
            {
                var go = new GameObject("__DATABASE__");
                instance = go.AddComponent<Database>();
            }
        }

        void OnLateUpdate()
        {
            
        }

        /// <summary>
        /// Get store
        /// </summary>
        /// <param name="branchName"></param>
        /// <param name="entityName"></param>
        /// <typeparam name="TState"></typeparam>
        /// <returns></returns>
        public static DatabaseVirtualStore<TState> GetStore<TState>(string entityName, TState initialState)
        {
            var branchSource = new DatabaseBranchSource( masterBranchName);
            var entitySource = new DatabaseEntitySource(entityName);
            var selector = new DatabaseSelector(branchSource, entitySource);
            return new DatabaseVirtualStore<TState>(selector, initialState);
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

        public static Action SubscribeStore<TState>(string branchName, string entityName, StoreHandler<TState> handler)
        {
            Action unsubscribeBranch = null;
            Action unsubscribeDepth2 = null;
            Action unsubscribeDepth3 = null;
            Action unsubscribeDepth4 = null;
            unsubscribeBranch = SubscribeBranch(branchName, delegate(DatabaseBranch branch)
            {
                unsubscribeDepth2?.Invoke();
                unsubscribeDepth3?.Invoke();
                unsubscribeDepth4?.Invoke();
                if (branch == null)
                {
                    // TODO: Fallback
                    // Subscribe to the parent branch if it has it
                    if (branchName.IndexOf('/') > 0)
                    {
                        // Subscribe to the parent branch but the same entity
                        unsubscribeDepth2 = SubscribeStore<TState>(branchName.Substring(0, branchName.LastIndexOf('/') - 1), entityName, handler);
                    } else if (branchName != masterBranchName)
                    {
                        // Subscribe master
                        unsubscribeDepth2 = SubscribeStore<TState>(masterBranchName, entityName, handler);
                    }
                    else
                    {
                        // Store does not exist, fallback null
                        handler(null);
                    }
                }
                else
                {
                    unsubscribeDepth2 = branch.SubscribeEntity(entityName, delegate(DatabaseEntity entity)
                    {
                        unsubscribeDepth3?.Invoke();
                        unsubscribeDepth4?.Invoke();
                        if (entity == null)
                        {
                            // TODO: Fallback
                            if (entityName.IndexOf('/') > 0)
                            {
                                // Subscribe to the same branch but parent entity
                                unsubscribeDepth3 = SubscribeStore<TState>(branchName, entityName.Substring(0, entityName.LastIndexOf('/') - 1), handler);
                            }
                            else
                            {
                                // Subscribe to the parent branch if it has it
                                if (branchName.IndexOf('/') > 0)
                                {
                                    // Subscribe to the parent branch but the same entity
                                    unsubscribeDepth3 = SubscribeStore<TState>(branchName.Substring(0, branchName.LastIndexOf('/') - 1), entityName, handler);
                                } else if (branchName != masterBranchName)
                                {
                                    // Subscribe master
                                    unsubscribeDepth3 = SubscribeStore<TState>(masterBranchName, entityName, handler);
                                }
                                else
                                {
                                    // Store does not exist, fallback null
                                    handler(null);
                                }
                            }
                        }
                        else
                        {
                            unsubscribeDepth3 = entity.SubscribeStore<TState>(delegate(DatabaseStore<TState> store)
                            {
                                unsubscribeDepth4?.Invoke();
                                if (store == null)
                                {
                                    // Seatch store in parent entity
                                    if (entityName.IndexOf('/') > 0)
                                    {
                                        // Subscribe to the same branch but parent entity
                                        unsubscribeDepth4 = SubscribeStore<TState>(branchName, entityName.Substring(0, entityName.LastIndexOf('/') - 1), handler);
                                    }
                                    else
                                    {
                                        // Subscribe to the parent branch if it has it
                                        if (branchName.IndexOf('/') > 0)
                                        {
                                            // Subscribe to the parent branch but the same entity
                                            unsubscribeDepth4 = SubscribeStore<TState>(branchName.Substring(0, branchName.LastIndexOf('/') - 1), entityName, handler);
                                        } else if (branchName != masterBranchName)
                                        {
                                            // Subscribe master
                                            unsubscribeDepth4 = SubscribeStore<TState>(masterBranchName, entityName, handler);
                                        }
                                        else
                                        {
                                            // Store does not exist, fallback null
                                            handler(null);
                                        }
                                    }
                                }
                                else
                                {
                                    handler(store);
                                }
                            });
                        }
                    });
                }
            });

            return () =>
            {
                unsubscribeDepth4?.Invoke();
                unsubscribeDepth3?.Invoke();
                unsubscribeDepth2?.Invoke();
                unsubscribeBranch?.Invoke();
            };
        }
        
        /// <summary>
        /// Subscribe branch change
        /// </summary>
        /// <param name="branchName"></param>
        /// <param name="handler"></param>
        /// <returns></returns>
        public static Action SubscribeBranch(string branchName, BranchHandler handler)
        {
            DatabaseBranch branch = null;
            m_Branches.TryGetValue(branchName, out branch);
            BranchOperationHandler branchOperationHandler = (name, branch, operation) =>
            {
                if (name == branchName)
                {
                    handler(branch);
                }
            };
            OnBranchOperation += branchOperationHandler;
            handler(branch);

            return () =>
            {
                OnBranchOperation -= branchOperationHandler;
            };
        }

        public static DatabaseBranch TouchBranch(string branchName)
        {
            DatabaseBranch branch = null;
            if (m_Branches.TryGetValue(branchName, out branch))
            {
                return branch;
            }
            Debug.Log($"Create new database branch {branchName}");
            branch = new DatabaseBranch(branchName);
            m_Branches[branchName] = branch;
            OnBranchOperation?.Invoke(branchName, branch, BranchOperation.CREATE);
            return branch;
        }
    }
}