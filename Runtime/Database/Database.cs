using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Metamatic
{
    
    public class Database: MonoBehaviour
    {
        public struct Address
        {
            public string branch;
            public string entity;
        }
        public const string ADDRESS_ENTITY_SEP = "/_E/";
        public const string ADDRESS_STORE_SEP = "/_S/";
        public static Database instance { get; private set; }
        public static event Action OnLifecycleEndFrame;
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
        public static event Action OnInitialize;
        public static DatabaseBranchSource currentBranchSource = new DatabaseBranchSource(masterBranchName);
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            OnInitialize?.Invoke();
            Database.Log($"Initialize");
            if (instance == null)
            {
                var go = new GameObject("__DATABASE__");
                instance = go.AddComponent<Database>();
            }
        }

        void LateUpdate()
        {
            OnLifecycleEndFrame?.Invoke();
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
            var branchSource = new DatabaseBranchSource( currentBranchSource);
            var entitySource = new DatabaseEntitySource(entityName);
            var selector = new DatabaseSelector(branchSource, entitySource);
            return new DatabaseVirtualStore<TState>(selector, initialState);
        }

        public static void Log(string message)
        {
            Debug.Log($"<color=red>DATABASE</color>: {message}");
        }

        
        
        /// <summary>
        /// Get branch selector by name
        /// </summary>
        /// <param name="branchName"></param>
        /// <returns></returns>
        public static DatabaseSelector Branch(string branchName)
        {
            return new DatabaseSelector(new DatabaseBranchSource(branchName));
        }

        public static DatabaseSelector Use(Behaviour behaviour)
        {
            if (behaviour is DatabaseContextBehaviour cb)
            {
                return new DatabaseSelector(cb.branchSource, cb.entitySource);
            } 
            var gameObjectEntity = behaviour.GetComponentInParent<Entity>();
            if (gameObjectEntity == null)
            {
                return new DatabaseSelector(new DatabaseEntitySource(behaviour.gameObject.name));
            }
            return new DatabaseSelector(new DatabaseEntitySource(gameObjectEntity.observableName));
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
            var gameObjectEntity = behaviour.GetComponentInParent<Entity>();
            if (gameObjectEntity == null)
            {
                return new DatabaseSelector(new DatabaseEntitySource(behaviour.gameObject.name));
            }
            return new DatabaseSelector(new DatabaseEntitySource(gameObjectEntity.observableName));
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
                var xkey = UnityEngine.Random.Range(1, 10000);
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
            var id = UnityEngine.Random.Range(0, 222222222);
            m_Branches.TryGetValue(branchName, out branch);
            BranchOperationHandler branchOperationHandler = (name, branch, operation) =>
            {
                Debug.Log($"BranchOperationHandler (${id}): {name}, {operation}");
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

        public static bool BranchExists(string branchName)
        {
            return m_Branches.ContainsKey(branchName);
        }

        public static bool RemoveBranch(string branchName)
        {
            DatabaseBranch branch = null;
            if (m_Branches.TryGetValue(branchName, out branch))
            {
                branch.Dispose();
                m_Branches.Remove(branchName);
                OnBranchOperation?.Invoke(branchName, null, BranchOperation.REMOVE);
                return true;
            }

            return false;
        }

        public static void Clear()
        {
            foreach (var branchName in m_Branches.Keys.ToArray())
            {
                RemoveBranch(branchName);
            }

            m_Branches.Clear();
        }
        
        /// <summary>
        /// Save database to the filesystem (by the default only master branch will be saved)
        /// </summary>
        public static bool Save()
        {
            foreach (var branch in m_Branches)
            {
                if (branch.Key == "master")
                {
                    Database.Log("Autosave master branch");
                    branch.Value.Save();
                }
            }

            return true;
        }

        public static void SaveStore<TState>(DatabaseStore<TState> store)
        {
            Database.Log($"Save to {store.address} data: {JsonUtility.ToJson(store.state)}");
            Repository.Save<TState>(store.address, store.state);
        }

        private static List<string> _storeLoadAttempts = new List<string>();
        
        /// <summary>
        /// Load story state from the filesystem
        /// </summary>
        /// <param name="address">Address of the entity</param>
        /// <param name="force">Ignore previous attempts and load again</param>
        /// <returns></returns>
        public static void TryLoadStore<TState>(string address, bool force = false, TState defaultState = default)
        {
            var typeFullname = typeof(TState).FullName;
            var fullAddress = $"{address}{ADDRESS_STORE_SEP}{typeFullname}";
            if (!force && _storeLoadAttempts.Contains(fullAddress))
            {
                return;
            }
            Database.Log($"Load {fullAddress}");
            _storeLoadAttempts.Add(fullAddress);
            if (Repository.Exists<TState>(fullAddress))
            {
                var loadedState = Repository.Load<TState>(fullAddress);
                var adressConfig = ParseAddress(address);
                Database.Log($"Store {address} sucessfully loaded: {JsonUtility.ToJson(loadedState)}");
                Database.TouchBranch(adressConfig.branch).TouchEntity(adressConfig.entity)
                    /* Initialize state without modifier to get every subscriber have only one subscription call */
                    .TouchStore<TState>(ref loadedState);
            }
        }
        
        /// <summary>
        /// Parse address and get branch and entity of it
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static Address ParseAddress(string address)
        {
            var parts = address.Split(ADDRESS_ENTITY_SEP, StringSplitOptions.None);
            return new Address
            {
                branch = parts[0],
                entity = parts[1]
            };
        }
        
        #if UNITY_EDITOR
        [MenuItem("Database/Open files location")]
        public static void MenuOpenDatabaseFolder()
        {
            var location = Application.persistentDataPath.Replace("/", "\\");
            Debug.Log($"Open: {location}");
            System.Diagnostics.Process p = new System.Diagnostics.Process();
            p.StartInfo = new System.Diagnostics.ProcessStartInfo("explorer.exe");
            p.StartInfo.Arguments = location;
            p.Start();
        }
        #endif
        
        void OnApplicationQuit()
        {
            // Save();
        }
    }
}