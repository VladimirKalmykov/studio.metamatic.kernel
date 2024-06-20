using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using Cysharp.Threading.Tasks;
using DuckJumper2024;
using Metamatic.Actions;
using Metamatic.Events;
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
    public class Repository: IDisposable
    {
        #region Exceptions

        /// <summary>
        /// This exception means that current request should be retrying
        /// </summary>
        public class RetryIsRequiredException : Exception
        {
            public readonly float delay;
            public RetryIsRequiredException(float delay)
            {
                this.delay = delay;
            }
        }
        
        public class DisableStorageIsRequiredException : Exception
        {
            public readonly float duration;
            public readonly Exception causeException;
            public DisableStorageIsRequiredException(float duration, Exception causeException) : base("Disable storage for " + duration + " seconds")
            {
                this.duration = duration;
                this.causeException = causeException;
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
                Set,
                Delete
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
                    _instance = new Repository(true, true);
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
        /// <summary>
        /// Maximum retry count for network operations
        /// </summary>
        public int maxRetryCount { get; set; } = 2;
        /// <summary>
        /// Delay before retrying the request
        /// </summary>
        public float retryDelay { get; set; } = 1f;
        /// <summary>
        /// Disable storage for some time if the request is failed
        /// </summary>
        public float disableStorageDuration { get; set; } = 60f;
        public readonly VersionControlStorageDriver inMemoryStorage;
        public VersionControlStorageDriver deviceStorage { get; private set; }
        public VersionControlStorageDriver remoteStorage { get; private set; }
        public CancellationToken disposeToken => m_DisposeCancellationTokenSource.Token;
        #endregion
        
        #region Private properties

        private readonly bool m_IsGlobalInstance;
        private readonly bool m_AutoSync;
        private Dictionary<(string address, Type dataType, Type metadataType), (float time, float responseDuration, int retryCount, NetworkOperationStatus status, Exception exception)> m_ActiveRequests = new();
        private readonly Dictionary<(Type, string), Store> m_Stores = new ();
        private readonly List<VersionControlStorageDriver> m_Drivers = new ();
        public IReadOnlyList<VersionControlStorageDriver> drivers => m_Drivers.AsReadOnly();
        private CancellationTokenSource m_DisposeCancellationTokenSource = new ();
        public DataSynchronizer synchronizer { get; private set; }
        private event Action<Store> OnAddStore;
        private event Action<Store> OnRemoveStore;
        private bool m_Disposed = false;
        #endregion
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="isGlobal">Indicates that this repository is singleton</param>
        /// <param name="autoSync">Auto sync all stores on Dispatcher's SystemEvent.Savepoint event</param>
        public Repository(bool isGlobal = false, bool autoSync = false)
        {
            m_IsGlobalInstance = isGlobal;
            m_AutoSync = autoSync;
            /* Initialize memory storage */
            inMemoryStorage = new VersionControlStorageDriver(this, new LocalMemoryStorage());
            Initialization(disposeToken);
        }
        
        private async void Initialization(CancellationToken token)
        {
            /* Add memory storage to the list of storages */
            m_Drivers.Add(inMemoryStorage);
            /* Load additional storages from project settings  */
            if (m_IsGlobalInstance)
            {
                LoadStoragesFromProjectSettings();
            }
            
            /* Add storages synchronizer */
            synchronizer = new DataSynchronizer(this);
            /* Subscribe to events */
            using var savepointEvent = m_AutoSync
                ? Dispatcher.global.Subscribe<SystemEvent>(evt =>
                {
                    if (evt == SystemEvent.Savepoint)
                    {
                        SyncAll();
                    }
                })
                : DisposableAction.Empty;
            await token;
        }

        /// <summary>
        /// Get store for specific type and address
        /// </summary>
        /// <param name="address"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public IStore<T> CreateStore<T>(string address, Store<T>.GetDefaultValue getDefaultValue)
        {
            var key = (typeof(T), address);
            if (m_Stores.ContainsKey(key))
            {
                throw new Exception($"Store for {typeof(T)} with address {address} already exists");
            }
            m_Stores[key] = new Store<T>(this, address, getDefaultValue);

            return new ProxyStore<T>((Store<T>)m_Stores[key]);
        }

        public async UniTask<IStore<T>> CreateStore<T>(string address, Store<T>.GetDefaultValue getDefaultValue, CancellationToken cancellationToken)
        {
            var store = CreateStore<T>(address, getDefaultValue);
            await store.WhenSynchronized(cancellationToken);
            return store;
        }
        
        /// <summary>
        /// Get store asynchronously. It will wait until store will be created and synchronized. To create store use CreateStore method.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="cancellationToken"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public async UniTask<IStore<T>> GetStore<T>(string address, CancellationToken cancellationToken)
        {
            var store = await GetStoreInternal<T>(address, cancellationToken);
            await store.WhenSynchronized(cancellationToken);
            return store;
        }
        
        /// <summary>
        /// TODO: Consider refactoring of subscription mechanism, it may be more efficient if it will use Dictionary (Type, string) key
        /// </summary>
        /// <param name="address"></param>
        /// <param name="cancellationToken"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private UniTask<IStore<T>> GetStoreInternal<T>(string address, CancellationToken cancellationToken)
        {
            if (m_Stores.TryGetValue((typeof(T), address), out var store))
            {
                return new UniTask<IStore<T>>(new ProxyStore<T>((Store<T>)store));
            }
            var taskCompletionSource = new UniTaskCompletionSource<IStore<T>>();
            Action<Store> handler = null;
            handler = store =>
            {
                if (store is Store<T> typedStore && typedStore.address == address)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }
                    OnAddStore -= handler;
                    taskCompletionSource.TrySetResult(new ProxyStore<T>(typedStore));
                }
            };
            OnAddStore += handler;
            cancellationToken.Register(() =>
            {
                OnAddStore -= handler;
                taskCompletionSource.TrySetCanceled();
            });
            return taskCompletionSource.Task;
        }
        
        public void AddStorage(VersionControlStorageDriver storage)
        {
            m_Drivers.Add(storage);
        }
        
        public void RemoveStorage(VersionControlStorageDriver storage)
        {
            m_Drivers.Remove(storage);
        }
        
        /// <summary>
        /// Sync all stores
        /// </summary>
        /// <param name="cancellationToken"></param>
        public void SyncAll(CancellationToken cancellationToken = default)
        {
            Logger.Log("Sync all stores");
            var combinedCancellation = CancellationTokenSource.CreateLinkedTokenSource(disposeToken, cancellationToken);
            foreach (var store in m_Stores.Values)
            {
                store.Sync(combinedCancellation.Token).Forget();
            }
        }
        
        private void LoadStoragesFromProjectSettings()
        {
            /* Load storages configuration */
            var settings = Resources.Load<RepositorySettings>("Settings/Repository");
            var platforms = settings != null ? settings.platforms : new PlatformSource[] { };
            foreach (var platform in platforms)
            {
                /* Add remote storage */
                if (platform.platform == Application.platform && platform.remoteStorage.enabled)
                {
                    var sourceType = Type.GetType(platform.remoteStorage.storage);
                    if (sourceType != null)
                    {
                        var storage = (Storage)Activator.CreateInstance(sourceType);
                        storage.locationType = Storage.LocationType.Remote;
                        var driver = new VersionControlStorageDriver(this, storage);
                        remoteStorage = driver;
                        m_Drivers.Add(driver);
                        break;
                    }
                    else
                    {
                        Debug.LogError("Source class not found");
                    }
                }
                
                /* Add local storage */
                if (platform.platform == Application.platform && platform.localStorage.enabled)
                {
                    var sourceType = Type.GetType(platform.localStorage.storage);
                    if (sourceType != null)
                    {
                        var storage = (Storage)Activator.CreateInstance(sourceType);
                        storage.locationType = Storage.LocationType.Local;
                        var driver = new VersionControlStorageDriver(this, storage);
                        deviceStorage = driver;
                        m_Drivers.Add(driver);
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
            Dispose();
        }

        public void Dispose()
        {
            if (m_Disposed) return;
            m_Disposed = true;
            m_DisposeCancellationTokenSource.Cancel();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// The method is responsible for logging and requests retrying of get requests
        /// </summary>
        /// <param name="address"></param>
        /// <param name="task"></param>
        /// <param name="retryCount"></param>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TMetadata"></typeparam>
        /// <returns></returns>
        /// tryRequiredException"></exception>
        public async UniTask<(T, Storage.Metadata, TMetadata)> ManageGetRequest<T, TMetadata>(Storage storage, string address, UniTask<(T, Storage.Metadata, TMetadata)> task, int retryCount = 0)
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
            requestMonitor.Update(requestStats);
            
            try
            {
                
                var result = await task;
                requestStats.responseDuration = Time.realtimeSinceStartup - startTime;
                requestStats.status = NetworkOperationStatus.Completed;
                requestMonitor.Release(requestStats);
                return result;
            }
            catch (Storage.TimeoutException e)
            {
                requestStats.responseDuration = Time.realtimeSinceStartup - startTime;
                requestStats.status = NetworkOperationStatus.Retry;
                requestStats.exception = e;
                requestMonitor.Release(requestStats);
                if (retryCount < maxRetryCount)
                {
                    throw new RetryIsRequiredException(retryDelay);
                }
                
                // TODO: Consider increase the time of disabling storage on each retry
                throw new DisableStorageIsRequiredException(disableStorageDuration, e);
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

                if (e is not Storage.DisabledException && e is not Storage.NotFoundException)
                {
                    // TODO: Consider increase the time of disabling storage on each retry
                    throw new DisableStorageIsRequiredException(disableStorageDuration, e);
                }

                throw;
            }
        }
        
        public async UniTask ManageSetRequest<T, TMetadata>(Storage storage, string address, UniTask task, int retryCount = 0)
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
            
            var requestMonitor = RequestMonitor.Create(this, storage, RequestMonitor.RequestType.Set, address, typeof(T), requestStats);
            requestMonitor.Update(requestStats);
            
            try
            {
                await task;
                requestStats.responseDuration = Time.realtimeSinceStartup - startTime;
                requestStats.status = NetworkOperationStatus.Completed;
                requestMonitor.Release(requestStats);
            }
            catch (Storage.TimeoutException e)
            {
                requestStats.responseDuration = Time.realtimeSinceStartup - startTime;
                requestStats.status = NetworkOperationStatus.Retry;
                requestStats.exception = e;
                requestMonitor.Release(requestStats);
                if (retryCount < maxRetryCount)
                {
                    throw new RetryIsRequiredException(retryDelay);
                }
                
                // TODO: Consider increase the time of disabling storage on each retry
                throw new DisableStorageIsRequiredException(disableStorageDuration, e);
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
                
                if (e is not Storage.DisabledException && e is not Storage.NotFoundException)
                {
                    // TODO: Consider increase the time of disabling storage on each retry
                    throw new DisableStorageIsRequiredException(disableStorageDuration, e);
                }

                throw;
            }
        }
        
        public async UniTask ManageDeleteRequest<T>(Storage storage, string address, UniTask task, int retryCount = 0)
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
            
            var requestMonitor = RequestMonitor.Create(this, storage, RequestMonitor.RequestType.Delete, address, typeof(T), requestStats);
            requestMonitor.Update(requestStats);
            
            try
            {
                await task;
                requestStats.responseDuration = Time.realtimeSinceStartup - startTime;
                requestStats.status = NetworkOperationStatus.Completed;
                requestMonitor.Release(requestStats);
            }
            catch (Storage.TimeoutException e)
            {
                requestStats.responseDuration = Time.realtimeSinceStartup - startTime;
                requestStats.status = NetworkOperationStatus.Retry;
                requestStats.exception = e;
                requestMonitor.Release(requestStats);
                if (retryCount < maxRetryCount) // TODO: Use some global configuration about max retry count
                {
                    throw new RetryIsRequiredException(retryDelay);
                }
                
                // TODO: Consider increase the time of disabling storage on each retry
                throw new DisableStorageIsRequiredException(disableStorageDuration, e);
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
                
                if (e is not Storage.DisabledException && e is not Storage.NotFoundException)
                {
                    // TODO: Consider increase the time of disabling storage on each retry
                    throw new DisableStorageIsRequiredException(disableStorageDuration, e);
                }

                throw;
            }
        }
    }
}