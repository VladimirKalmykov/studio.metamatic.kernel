using System;
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
        
        public delegate UniTask<bool> ConnectionExceptionHook(IOOperationConfig config, IOSource source, Exception exception, CancellationToken cancellationToken);
        
        public readonly struct IOOperationConfig
        {
            public readonly string address;
            public readonly float timeout;
            public IOOperationConfig(string address, float timeout = 0f)
            {
                this.address = address;
                this.timeout = timeout;
            }
        }
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
        
        public static event Action<IOOperationConfig> OnIOOperationError;
        /// <summary>
        /// Exception hook is called when connection exception is thrown. If hook returns true, the operation will be repeated.
        /// </summary>
        private readonly MicroState<ConnectionExceptionHook> m_ConnectionExceptionHook = new MicroState<ConnectionExceptionHook>(null);
        /// <summary>
        /// Contains task that is called when connection exception is thrown. If task returns true, the operation will be repeated.
        /// </summary>
        private UniTask<bool>? m_ConnectionExceptionHookTask;
        
        
        public readonly MicroState<IOSource> m_DefaultSource = new MicroState<IOSource>(null);

        public MicroStateReadOnly<IOSource> defaultSource => m_DefaultSource;
        
        public UniTask<T> Read<T>(IOOperationConfig config, CancellationToken token = default)
        {
            var timeoutCancellationSource = new CancellationTokenSource();
            var combinedCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(token, timeoutCancellationSource.Token);
            timeoutCancellationSource.CancelAfter(TimeSpan.FromSeconds(config.timeout));
            var task = defaultSource.value.Read<T>(config.address, combinedCancellationToken.Token);
            return InternalWrapIOTaskWithTimeoutException(task,  config, timeoutCancellationSource, token);
        }

        private async UniTask<T> InternalWrapIOTaskWithTimeoutException<T>(UniTask<T> task, IOOperationConfig config, CancellationTokenSource timeoutCancellationSource, CancellationToken cancellationToken)
        {
            try
            {
                var value = await task;
                return value;
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException && !timeoutCancellationSource.IsCancellationRequested)
                {
                    /* Operation just canceled */
                    throw new OperationCanceledException();
                }
                
                try
                {
                    var finalException = ex is OperationCanceledException && timeoutCancellationSource.IsCancellationRequested
                        ? new TimeoutException("Connection timeout")
                        : ex;
                    OnIOOperationError?.Invoke(config);
                    if (await HookUpIOOperation(config, finalException, cancellationToken))
                    {
                        /* Try to send request again */
                        await Read<T>(config, cancellationToken);
                    }
                    else
                    {
                        throw new ConnectionRejectionException(
                            "Connection rejected because of timeout and hook is not set");
                    }

                }
                catch (OperationCanceledException)
                {
                    throw new ConnectionRejectionException(
                        "Connection rejected because of timeout and hook is not set");
                }
                catch (Exception e)
                {
                    throw new ConnectionRejectionException(
                        $"Connection rejected because of error: {e.Message}");
                }
                throw new ConnectionRejectionException(
                    $"Unexpected connection error: {ex.Message}");
            }
        }

        
        
        public UniTask Write<T>(IOOperationConfig config, T value, CancellationToken token = default)
        {
            var timeoutCancellationSource = new CancellationTokenSource();
            var combinedCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(token, timeoutCancellationSource.Token);
            timeoutCancellationSource.CancelAfter(TimeSpan.FromSeconds(config.timeout));
            var task = defaultSource.value.Write<T>(config.address, value, combinedCancellationToken.Token);
            return InternalWrapIOTaskWithTimeoutException(task, config, value, timeoutCancellationSource, token);
        }
        
        private async UniTask InternalWrapIOTaskWithTimeoutException<T>(UniTask task, IOOperationConfig config, T value, CancellationTokenSource timeoutCancellationSource, CancellationToken cancellationToken)
        {
            try
            {
                await task;
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException && !timeoutCancellationSource.IsCancellationRequested)
                {
                    /* Operation just canceled */
                    throw new OperationCanceledException();
                }
                
                try
                {
                    var finalException = ex is OperationCanceledException && timeoutCancellationSource.IsCancellationRequested
                        ? new TimeoutException("Connection timeout")
                        : ex;
                    OnIOOperationError?.Invoke(config);
                    if (await HookUpIOOperation(config, finalException, cancellationToken))
                    {
                        /* Try to send request again */
                        await Write(config, value, cancellationToken);
                    }
                    else
                    {
                        throw new ConnectionRejectionException(
                            "Connection rejected because of timeout and hook is not set");
                    }

                }
                catch (OperationCanceledException)
                {
                    throw new ConnectionRejectionException(
                        "Connection rejected because of timeout and hook is not set");
                }
                catch (Exception e)
                {
                    throw new ConnectionRejectionException(
                        $"Connection rejected because of error: {e.Message}");
                }
                throw new ConnectionRejectionException(
                    $"Unexpected connection error: {ex.Message}");
            }
        }
        
        private async UniTask<bool> HookUpIOOperation(IOOperationConfig config, Exception exception, CancellationToken cancellationToken)
        {
            var timeoutCancellationTokenSource = new CancellationTokenSource();
            try
            {
                var combinedCancellationTokenSource =
                    CancellationTokenSource.CreateLinkedTokenSource(cancellationToken,
                        timeoutCancellationTokenSource.Token);
                timeoutCancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(5));
                var hook = await m_ConnectionExceptionHook.WaitUntilValueIsNotNull(
                    combinedCancellationTokenSource.Token);
                bool hookResult;
                if (!m_ConnectionExceptionHookTask.HasValue)
                {
                    m_ConnectionExceptionHookTask = hook.Invoke(
                        config,
                        defaultSource.value,
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
                        m_DefaultSource.value = (IOSource) Activator.CreateInstance(sourceType);
                        // source.Initialize(platform.settings);
                        break;
                    }
                    else
                    {
                        Debug.LogError("Source class not found");
                    }
                }
            }
            if (defaultSource == null)
                m_DefaultSource.value = new LocalMemorySource();
            
            /* Display source class name */
            Debug.Log($"<color=red>Repository source</color>: {defaultSource.GetType().Name}");
        }

        protected override void RunAsync(CancellationToken cancellationToken)
        {
            
        }
    }
}