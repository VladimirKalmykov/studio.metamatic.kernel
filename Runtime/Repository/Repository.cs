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
        #region Exceptions

        /// <summary>
        /// This exception means that current request should be retrying
        /// </summary>
        public class RetryRequiredException : Exception
        {
            public readonly int retryCount;
            public RetryRequiredException(int retryCount)
            {
                this.retryCount = retryCount;
            }
        }
        #endregion
        
        #region Structs
        public enum NetworkOperationStatus
        {
            Pending,
            Cancelled,
            Completed,
            Retry,
            Failed
        }
        
        /// <summary>
        /// TODO: Make MonitoredRequest class to be created from pool
        /// </summary>
        public class RequestMonitor : IDisposable
        {
            public enum RequestType
            {
                Get,
                Set
            }
            public struct Stats
            {
                public float startTime;
                public float responseDuration;
                public NetworkOperationStatus status;
                public Exception exception;
                public int retryCount;
            }
            public Repository repository { get; private set; }
            public Storage storage { get; private set; }
            public Stats stats { get; private set; }
            public RequestType requestType { get; private set; }
            public string address { get; private set; }
            public Type dataType { get; private set; }
            
            private event Action OnDispose;
            private event Action OnUpdate;
            private bool m_Disposed = false;

            public static RequestMonitor Create(Repository repository, Storage storage, RequestType type, string address, Type dataType, Stats initialStats)
            {
                return new RequestMonitor(repository, storage, type, address, dataType, initialStats);
            }
            
            public RequestMonitor(Repository repository, Storage storage, RequestType type, string address, Type dataType, Stats initialStats)
            {
                this.repository = repository;
                this.storage = storage;
                requestType = type;
                this.address = address;
                this.dataType = dataType;
                this.stats = initialStats;
                
#if METAMATIC_TERMINAL
                using var terminalLog = Terminal.CreateUpdatableLog(
                $"<b>Repository</b>: {dataType} <color=#00e6e6>{address}</color> <{dataType.Name}> to <color=#00e6e6>{storage.GetType().Name}</color>: {{0}} (retry: {initialStats.retryCount})", true);
                Action updateHandler = () => {
                    terminalLog.Update(stats.status.ToString());
                };
                OnUpdate += updateHandler;
                OnDispose += () => {
                    OnUpdate -= updateHandler;
                    terminalLog.Persist();    
                };
#endif
            }

            public void Update(Stats stats)
            {
                this.stats = stats;
                OnUpdate?.Invoke();
            }

            public void Release(Stats stats)
            {
                this.stats = stats;
                Dispose();
            }

            public void Dispose()
            {
                if (m_Disposed) return;
                m_Disposed = true;
                OnDispose?.Invoke();
            }
        }
        #endregion
        
        #region Global
        public static readonly string SettingsPath = "Assets/Metamatic/Resources/Settings/Repository.asset";
        private static Repository _instance;
        public static Repository global
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new Repository(true);
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
        public readonly VersionControlStorageDriver inMemoryStorage;
        #endregion
        
        #region Private properties

        private readonly bool m_IsGlobalInstance;
        private Dictionary<(string address, Type dataType, Type metadataType), (float time, float responseDuration, int retryCount, NetworkOperationStatus status, Exception exception)> m_ActiveRequests = new();
        private readonly Dictionary<(Type, string), Store> m_Stores = new ();
        private readonly List<VersionControlStorageDriver> m_Storages = new (null);
        private CancellationTokenSource m_DisposeCancellationTokenSource = new ();
        public DataSynchronizer synchronizer { get; private set; }
        #endregion
        
        public Repository(bool isGlobal = false)
        {
            m_IsGlobalInstance = isGlobal;
            /* Initialize memory storage */
            inMemoryStorage = new VersionControlStorageDriver(this, new LocalMemoryStorage());
            Initialization(m_DisposeCancellationTokenSource.Token);
        }

        /// <summary>
        /// Get store for specific type and address
        /// </summary>
        /// <param name="address"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public IStore<T> GetStore<T>(string address)
        {
            var key = (typeof(T), address);
            if (!m_Stores.ContainsKey(key))
            {
                m_Stores[key] = new Store<T>(this, address);
            }

            return new ProxyStore<T>((Store<T>)m_Stores[key]);
        }
        
        public void AddStorage(VersionControlStorageDriver storage)
        {
            m_Storages.Add(storage);
        }
        
        public void RemoveStorage(VersionControlStorageDriver storage)
        {
            m_Storages.Remove(storage);
        }

        public async UniTask<IStore<T>> GetStore<T>(string address, CancellationToken cancellationToken)
        {
            var store = GetStore<T>(address);
            await store.WhenSynchronized(cancellationToken);
            return store;
        }
        
        private async void Initialization(CancellationToken token)
        {
            /* Add memory storage to the list of storages */
            m_Storages.Add(inMemoryStorage);
            /* Load additional storages from project settings  */
            if (m_IsGlobalInstance)
            {
                LoadStoragesFromProjectSettings();
            }
            
            /* Add storages synchronizer */
            synchronizer = new DataSynchronizer(this, m_Storages.ToArray());
            await token;
        }
        
        private void LoadStoragesFromProjectSettings()
        {
            /* Load storages configuration */
            var settings = Resources.Load<RepositorySettings>("Settings/Repository");
            var platforms = settings != null ? settings.platforms : new PlatformSource[] { };
            foreach (var platform in platforms)
            {
                /* Add remote storage */
                if (platform.platform == Application.platform && platform.enableRemoteStorage)
                {
                    var sourceType = Type.GetType(platform.remoteStorage);
                    if (sourceType != null)
                    {
                        var storage = (Storage)Activator.CreateInstance(sourceType);
                        storage.locationType = Storage.LocationType.Remote;
                        m_Storages.Add(new VersionControlStorageDriver(this, storage));
                        break;
                    }
                    else
                    {
                        Debug.LogError("Source class not found");
                    }
                }
                
                /* Add local storage */
                if (platform.platform == Application.platform && platform.enableLocalStorage)
                {
                    var sourceType = Type.GetType(platform.localStorage);
                    if (sourceType != null)
                    {
                        var storage = (Storage)Activator.CreateInstance(sourceType);
                        storage.locationType = Storage.LocationType.Local;
                        m_Storages.Add(new VersionControlStorageDriver(this, storage));
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
        
        /// <summary>
        /// The method is responsible for logging and requests retrying
        /// </summary>
        /// <param name="address"></param>
        /// <param name="task"></param>
        /// <param name="retryCount"></param>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TMetadata"></typeparam>
        /// <returns></returns>
        /// tryRequiredException"></exception>
        public async UniTask<(T, Storage.Metadata, TMetadata)> ManageRequest<T, TMetadata>(Storage storage, string address, UniTask<(T, Storage.Metadata, TMetadata)> task, int retryCount = 0)
        {
            var startTime = Time.realtimeSinceStartup;
            var requestStats = new RequestMonitor.Stats()
            {
                responseDuration = 0f,
                retryCount = retryCount,
                startTime = startTime,
                status = NetworkOperationStatus.Pending,
                exception = null
            };
            
            var requestMonitor = RequestMonitor.Create(this, storage, RequestMonitor.RequestType.Get, address, typeof(T), requestStats);
            requestMonitor.Release(requestStats);
            
            try
            {
                var result = await task;
                requestStats.responseDuration = Time.realtimeSinceStartup - startTime;
                requestStats.status = NetworkOperationStatus.Completed;
                requestMonitor.Update(requestStats);
                return result;
            }
            catch (Storage.TimeoutException e)
            {
                requestStats.responseDuration = Time.realtimeSinceStartup - startTime;
                requestStats.status = NetworkOperationStatus.Retry;
                requestStats.exception = e;
                requestMonitor.Release(requestStats);
                if (retryCount < 3) // TODO: Use some global configuration about max retry count
                {
                    throw new RetryRequiredException(requestStats.retryCount);
                }
                
                throw e;
            }
            catch (OperationCanceledException e)
            {
                requestStats.responseDuration = Time.realtimeSinceStartup - startTime;
                requestStats.status = NetworkOperationStatus.Cancelled;
                requestMonitor.Release(requestStats);
                throw;
            }
            catch (Exception e)
            {
                requestStats.responseDuration = Time.realtimeSinceStartup - startTime;
                requestStats.status = NetworkOperationStatus.Failed;
                requestStats.exception = e;
                requestMonitor.Release(requestStats);
                throw;
            }
        }
    }
}