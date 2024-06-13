using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic
{
    public class Repository: AsyncBehaviour
    {
        public class ConnectionRejectionException: Exception
        {
            public ConnectionRejectionException(string message): base(message)
            {
                
            }
        }
        
        [SerializeField] private bool isGlobal;
        
        public delegate UniTask<bool> ConnectionExceptionHook(Repository repository, IOOperation operation, IOSource source, Exception exception, CancellationToken cancellationToken);
        
        public static readonly string SettingsPath = "Assets/Metamatic/Resources/Settings/Repository.asset";
        private static Repository _Instance;
        public static Repository global
        {
            get
            {
                if (_Instance == null)
                {
                    _Instance = new GameObject("__GLOBALREPOSITORY__").AddComponent<Repository>();
                    _Instance.isGlobal = true;
#if UNITY_EDITOR
                    _Instance.gameObject.hideFlags = HideFlags.DontSaveInEditor & HideFlags.DontSave;
#endif
                    _Instance.InitializeGlobal();
                }
                return _Instance;
            }
        }
        
        public static event Action<IOOperation> OnIOOperationError;
        /// <summary>
        /// Exception hook is called when connection exception is thrown. If hook returns true, the operation will be repeated.
        /// </summary>
        private readonly MicroState<ConnectionExceptionHook> m_ConnectionExceptionHook = new MicroState<ConnectionExceptionHook>(null);
        /// <summary>
        /// Contains task that is called when connection exception is thrown. If task returns true, the operation will be repeated.
        /// </summary>
        private UniTask<bool>? m_ConnectionExceptionHookTask;
        private ObservableCollection<IOOperation> m_IOOperations = new ();
        
        
        public readonly MicroState<IOSource> m_CurrentSource = new MicroState<IOSource>(null);

        public MicroStateReadOnly<IOSource> currentSource => m_CurrentSource;
        public float timeout => 5f;
        
        public DisposableAction RegisterConnectionExceptionHook(ConnectionExceptionHook hook)
        {
            if (m_ConnectionExceptionHook.value != null)
            {
                throw new Exception("Connection exception hook is already set");
            }
            m_ConnectionExceptionHook.value = hook;
            
            return new DisposableAction(() =>
            {
                if (m_ConnectionExceptionHook.value == hook)
                    m_ConnectionExceptionHook.value = null;
            });
        }

        public UniTask<T> Read<T>(string address, CancellationToken token = default)
        {
            var operationConfig = new IOOperationConfig(address, IOOperationConfig.OperationType.Read, timeout);
            return Read<T>(operationConfig, token);
        }
        
        protected UniTask<T> Read<T>(IOOperationConfig config, CancellationToken token = default)
        {
            var operation = new IOReadOperation<T>(config);
            m_IOOperations.Add(operation);
            
            var timeoutCancellationSource = new CancellationTokenSource();
            var combinedCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(token, timeoutCancellationSource.Token);
            timeoutCancellationSource.CancelAfterSlim(TimeSpan.FromSeconds(config.timeout));
            operation.task = currentSource.value.Read<T>(config.address, combinedCancellationToken.Token);
            return InternalWrapIOTaskWithTimeoutException(operation, timeoutCancellationSource, token);
        }

        private async UniTask<T> InternalWrapIOTaskWithTimeoutException<T>(IOReadOperation<T> operation, CancellationTokenSource timeoutCancellationSource, CancellationToken cancellationToken)
        {
            try
            {
                operation.status = IOOperation.Status.Pending;
                var value = await operation.task;
                operation.status = IOOperation.Status.Done;
                return value;
            }
            catch (Exception ex)
            {
                if (ex is IOSource.NotFoundException)
                {
                    /* Not actually an error. It indicates that searched resource is not found. */
                    throw ex;
                }
                if (ex is OperationCanceledException && !timeoutCancellationSource.IsCancellationRequested)
                {
                    operation.status = IOOperation.Status.Cancelled;
                    ReleaseOperation(operation);
                    /* Operation just canceled */
                    throw new OperationCanceledException();
                }

                var retry = false;
                try
                {
                    var isTimeout = ex is OperationCanceledException &&
                                    timeoutCancellationSource.IsCancellationRequested;
                    var finalException = isTimeout
                        ? new TimeoutException("Connection timeout")
                        : ex;
                    operation.status = isTimeout ? IOOperation.Status.Timeout : IOOperation.Status.Error;
                    OnIOOperationError?.Invoke(operation);
                    retry = await HookUpIOOperation(operation, finalException, cancellationToken);
                    
                }
                catch (OperationCanceledException oce)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        operation.status = IOOperation.Status.Cancelled;
                        ReleaseOperation(operation);
                        throw;
                    }
                    else
                    {
                        operation.status = IOOperation.Status.Rejected;
                        ReleaseOperation(operation);
                        throw new ConnectionRejectionException(
                            $"Connection rejected because exception hook failed");
                    }
                }
                catch (Exception e)
                {
                    operation.status = IOOperation.Status.Rejected;
                    ReleaseOperation(operation);
                    throw new ConnectionRejectionException(
                        $"Connection rejected because of error: {e.Message}");
                }
                
                if (retry)
                {
                    operation.status = IOOperation.Status.Retrying;
                    ReleaseOperation(operation);
                        
                    /* Try to send request again */
                    var result = await Read<T>(operation.config, cancellationToken);
                    return result;
                }
                operation.status = IOOperation.Status.Rejected;
                ReleaseOperation(operation);
                throw new ConnectionRejectionException(
                    "Connection rejected without, because exception hook has returned false");
            }
        }
        
        protected UniTask Write<T>(IOOperationConfig config, T value, CancellationToken token = default)
        {
            var operation = new IOWriteOperation<T>(config, value);
            var timeoutCancellationSource = new CancellationTokenSource();
            var combinedCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(token, timeoutCancellationSource.Token);
            timeoutCancellationSource.CancelAfterSlim(TimeSpan.FromSeconds(config.timeout));
            operation.task = currentSource.value.Write<T>(config.address, value, combinedCancellationToken.Token);
            return InternalWrapIOTaskWithTimeoutException<T>(operation, value, timeoutCancellationSource, token);
        }
        
        public UniTask Write<T>(string address, T value, CancellationToken token = default)
        {
            var operationConfig = new IOOperationConfig(address, IOOperationConfig.OperationType.Write, timeout);
            return Write(operationConfig, value, token);
        }
        
        private async UniTask InternalWrapIOTaskWithTimeoutException<T>(
            IOWriteOperation<T> operation,
            T value,
            CancellationTokenSource timeoutCancellationSource,
            CancellationToken cancellationToken
        )
        {
            try
            {
                operation.status = IOOperation.Status.Pending;
                await operation.task;
                operation.status = IOOperation.Status.Done;
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException && !timeoutCancellationSource.IsCancellationRequested)
                {
                    operation.status = IOOperation.Status.Cancelled;
                    ReleaseOperation(operation);
                    /* Operation just canceled */
                    throw new OperationCanceledException();
                }
                
                var retry = false;
                try
                {
                    var isTimeout = ex is OperationCanceledException &&
                                    timeoutCancellationSource.IsCancellationRequested;
                    var finalException = isTimeout
                        ? new TimeoutException("Connection timeout")
                        : ex;
                    operation.status = isTimeout ? IOOperation.Status.Timeout : IOOperation.Status.Error;
                    OnIOOperationError?.Invoke(operation);
                    retry = await HookUpIOOperation(operation, finalException, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        operation.status = IOOperation.Status.Cancelled;
                        ReleaseOperation(operation);
                        throw;
                    }
                    else
                    {
                        operation.status = IOOperation.Status.Rejected;
                        ReleaseOperation(operation);
                        throw new ConnectionRejectionException(
                            $"Connection rejected because exception hook failed");
                    }
                }
                catch (Exception e)
                {
                    operation.status = IOOperation.Status.Rejected;
                    ReleaseOperation(operation);
                    throw new ConnectionRejectionException(
                        $"Connection rejected because of error: {e.Message}");
                }
                
                Debug.Log($"retry: {retry}");
                if (retry)
                {
                    operation.status = IOOperation.Status.Retrying;
                    ReleaseOperation(operation);
                    /* Try to send request again */
                    await Write(operation.config, value, cancellationToken);
                    return;
                }
                operation.status = IOOperation.Status.Rejected;
                ReleaseOperation(operation);
                throw new ConnectionRejectionException(
                    "Connection rejected because of hook");
            }
        }
        
        private async UniTask<bool> HookUpIOOperation(IOOperation operation, Exception exception, CancellationToken cancellationToken)
        {
            var timeoutCancellationTokenSource = new CancellationTokenSource();
            try
            {
                var combinedCancellationTokenSource =
                    CancellationTokenSource.CreateLinkedTokenSource(cancellationToken,
                        timeoutCancellationTokenSource.Token);
                timeoutCancellationTokenSource.CancelAfterSlim(TimeSpan.FromSeconds(5));
                var hook = await m_ConnectionExceptionHook.WaitUntilValueIsNotNull(
                    combinedCancellationTokenSource.Token);
                bool hookResult;
                if (!m_ConnectionExceptionHookTask.HasValue)
                {
                    m_ConnectionExceptionHookTask = hook.Invoke(
                        this,
                        operation,
                        currentSource.value,
                        exception,
                        cancellationToken
                    );
                    try
                    {
                        hookResult = await m_ConnectionExceptionHookTask.Value;
                    }
                    finally
                    {
                        m_ConnectionExceptionHookTask = null;
                    }
                }
                else
                {
                    hookResult = await m_ConnectionExceptionHookTask.Value;
                }

                Debug.Log($"hookResult: {hookResult}");
                return hookResult;
            }
            catch (OperationCanceledException)
            {
                if (timeoutCancellationTokenSource.IsCancellationRequested)
                {
                    throw new OperationCanceledException();
                }
            }
            
            throw new Exception("Exception hook has thrown exception");
        }
        
        private void ReleaseOperation(IOOperation operation)
        {
            operation.Dispose();
            m_IOOperations.Remove(operation);
        }

        private void InitializeGlobal()
        {
            /* Load settings */
            var settings = Resources.Load<RepositorySettings>("Settings/Repository");
            var platforms = settings != null ? settings.platforms : new PlatformSource[] { };
            foreach (var platform in platforms)
            {
                if (platform.platform == Application.platform)
                {
                    var sourceType = Type.GetType(platform.sourceClassName);
                    if (sourceType != null)
                    {
                        m_CurrentSource.value = (IOSource) Activator.CreateInstance(sourceType);
                        // source.Initialize(platform.settings);
                        break;
                    }
                    else
                    {
                        Debug.LogError("Source class not found");
                    }
                }
            }
            if (currentSource == null)
                m_CurrentSource.value = new LocalMemorySource();
            
            /* Display source class name */
            Debug.Log($"<color=red>Repository source</color>: {currentSource.GetType().Name}");
        }

        protected override async void RunAsync(CancellationToken cancellationToken)
        {
            #if DEBUG
            /* Track all operations */
            var operationsHandler = new SequentialArrayHandler<IOOperation>(OnOperationInit);
            m_IOOperations.CollectionChanged += (sender, args) =>
            {
                operationsHandler.Handle(m_IOOperations);
            };
            #endif
            await cancellationToken;
        }
        
        private async void OnOperationInit(IOOperation operation, CancellationToken token)
        {
            var methodText = operation.config.operationType == IOOperationConfig.OperationType.Read ? "Read" : "Write";
            using var terminalLog = Terminal.CreateUpdatableLog(
                $"<b>Repository</b>: {methodText} <color=#00e6e6>{operation.type.Name}@{operation.config.address}</color> to <color=#00e6e6>{currentSource.value.GetType().Name}</color>: {{0}} (retry: {operation.config.retryCount})", true);
            using var statusSubscription = operation.m_Status.Subscribe(status =>
            {
                terminalLog.Update(status.ToString());
            });
            await token;
        }
    }
}