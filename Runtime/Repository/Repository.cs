using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using Cysharp.Threading.Tasks;
using DuckJumper2024;
using Metamatic.StorageDriver;
using UnityEngine;
using UnityEngine.PlayerLoop;

namespace Metamatic
{
    /// <summary>
    /// Repository is responsible for handling all storage operations.
    /// It's a singleton that can be accessed from any part of the application.
    /// Storage agent can implement logic for handling network operations, like timeouts, retries, etc.
    /// Otherwise, it can be used as a simple wrapper for storage operations and, for example, logging.
    /// </summary>
    public class Repository
    {
        #region Global
        private static Repository _instance;
        public static Repository global
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new Repository();
                }
                return _instance;
            }
        }
        #endregion
        
        #region Properties
        /// <summary>
        /// Default timeout for network operations
        /// </summary>
        public float timeout { get; set; } = 10f;
        #endregion
        
        #region Private properties
        private ObservableCollection<NetworkOperation> m_Operations = new ();
        private readonly Dictionary<(Type, string), Store> m_Stores = new ();
        private readonly List<VersionControlStorageDriver> m_Storages = new List<VersionControlStorageDriver>(null);
        private CancellationTokenSource m_DisposeCancellationTokenSource = new ();
        public readonly VersionControlStorageDriver localStorage;
        public DataSynchronizer synchronizer { get; private set; }
        #endregion
        
        public Repository()
        {
            /* Initialize memory storage */
            localStorage = new VersionControlStorageDriver(this, new LocalMemoryStorage());
            Initialization(m_DisposeCancellationTokenSource.Token);
        }
        
        public Store<T> GetStore<T>(string name)
        {
            var key = (typeof(T), name);
            if (!m_Stores.ContainsKey(key))
            {
                m_Stores[key] = new Store<T>(this, name);
            }
            return (Store<T>)m_Stores[key];
        }
        
        private async void Initialization(CancellationToken token)
        {
            /* Add memory storage to the list of storages */
            m_Storages.Add(localStorage);
            /* Load additional storages from project settings  */
            LoadStoragesFromProjectSettings();
            
            /* Add storages synchronizer */
            synchronizer = new DataSynchronizer(this, m_Storages.ToArray());
            
            /* Track all operations in Terminal (only in DEBUG mode) */
            #if DEBUG
            var operationsHandler = new SequentialArrayHandler<NetworkOperation>(OnOperationInit);
            System.Collections.Specialized.NotifyCollectionChangedEventHandler HandleCollectionChange = (sender, args) =>
            {
                operationsHandler.Handle(m_Operations);
            };
            m_Operations.CollectionChanged += HandleCollectionChange;
            #endif
            
            await token;
            #if DEBUG
            m_Operations.CollectionChanged -= HandleCollectionChange;
            #endif
        }
        
        private void LoadStoragesFromProjectSettings()
        {
            /* Load storages configuration */
            var settings = Resources.Load<RepositorySettings>("Settings/Repository");
            var platforms = settings != null ? settings.platforms : new PlatformSource[] { };
            foreach (var platform in platforms)
            {
                if (platform.platform == Application.platform)
                {
                    var sourceType = Type.GetType(platform.remoteStorage);
                    if (sourceType != null)
                    {
                        m_Storages.Add(new VersionControlStorageDriver(this, (Storage)Activator.CreateInstance(sourceType)));
                        // source.Initialize(platform.settings);
                        break;
                    }
                    else
                    {
                        Debug.LogError("Source class not found");
                    }
                }
            }
        }
        
        ~Repository()
        {
            m_DisposeCancellationTokenSource.Cancel();
        }
        
        public void ReleaseOperation(NetworkOperation operation)
        {
            operation.Dispose();
            m_Operations.Remove(operation);
        }
        
        #if DEBUG
        private async void OnOperationInit(NetworkOperation operation, CancellationToken token)
        {
            var methodText = operation.config.method == StorageRequestConfig.Method.Read ? "Read" : "Write";
            using var terminalLog = Terminal.CreateUpdatableLog(
                $"<b>Repository</b>: {methodText} <color=#00e6e6>{operation.type.Name}@{operation.config.address}</color> to <color=#00e6e6>{operation.source.GetType().Name}</color>: {{0}} (retry: {operation.config.retryCount})", true);
            using var statusSubscription = operation.m_Status.Subscribe(status =>
            {
                terminalLog.Update(status.ToString());
            });
            await token;
            terminalLog.Persist();
        }
        #endif
    }
}