using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Metamatic.DevTools;
using UnityEngine;
using UnityEngine.Serialization;

namespace Metamatic
{
    public class InternalMicroOperation: IExposedDisposed
    {
        
        #if DEBUG
        private static int _LastInstanceID;
        private static int GetNextID()
        {
            return ++_LastInstanceID == int.MaxValue ? _LastInstanceID = 0 : _LastInstanceID;
        }
        private static int _InstanceCount;
        public readonly int ID;
        public int debugUseCount { get; private set; } = 1;
        
        [RuntimeInitializeOnLoadMethod]
        private static void Initialize()
        {
            UILog.Add("MicroOperation", "Instances count", () => _InstanceCount);
        }
        #endif
        public delegate UniTask AsyncAction(CancellationToken cancellationToken);
        public delegate UniTask AsyncOperationAction(InternalMicroOperation operation);
        
        private static readonly ParamLessPool<InternalMicroOperation> Pool2 = new (20);

        public static MicroOperation Call(AsyncAction action, object[] dependencies = null)
        {
            var operation = Pool2.Get();
#if DEBUG
            operation.debugUseCount++;
            _InstanceCount++;
#endif
            operation.dependencies = dependencies;
            operation.isDisposed = false;

            operation.InternalRun(action);
            return new MicroOperation(operation);
        }
        
        public static MicroOperation Run(AsyncOperationAction action, object[] dependencies = null, bool debug = false)
        {
            var operation = Pool2.Get();

#if DEBUG
            operation.isDebug = debug;
            operation.debugUseCount++;
            _InstanceCount++;
#endif
            operation.dependencies = dependencies;
            operation.isDisposed = false;

            operation.InternalRun(action);
            return new MicroOperation(operation);
        }

        /// <summary>
        /// Contains dependencies for the current operation. When it disposed all dependencies should not be null to dispose async.
        /// </summary>
        // ReSharper disable once InconsistentNaming
        private object[] dependencies;
        private CancellationTokenSource m_PerformCancellationTokenSource;
        /// <summary>
        /// This token is used to cancel async dispose. It is created when DisposeAsync is called.
        /// </summary>
        private CancellationTokenSource m_AsyncDisposeCancellationTokenSource;
        public CancellationToken cancellationToken => m_PerformCancellationTokenSource.Token;
        /// <summary>
        /// CancellationToken for async dispose. It will cancel async dispose.
        /// </summary>
        public CancellationToken asyncDisposeCancellationToken => m_AsyncDisposeCancellationTokenSource?.Token ?? CancellationToken.None;
        /// <summary>
        /// Contains task that is returned by action. This task is used only for async dispose.
        /// </summary>
        private UniTask? m_ExecuteTask;
        /// <summary>
        /// Will contain all dependencies that are IExposedDisposed. If any of the dependencies is disposed, async disposing of the operation will be cancelled.
        /// </summary>
        private List<IExposedDisposed> m_ExposedDisposedDependencies;
        /// <summary>
        /// Will contain all dependencies that does not implement IExposedDisposed. If any of the dependencies is null, async disposing of the operation will be cancelled.
        /// </summary>
        private List<object> m_NotExposedDisposedDependencies;
        /// <summary>
        /// Determines if operation is concluding without aborting by dependency invalidation.
        /// </summary>
        public bool isConcluded { get; private set; }
        /// <summary>
        /// Determines if operation is aborted by dependency invalidation.
        /// </summary>
        public bool isAborted => !isConcluded;
#if DEBUG
        public bool isDebug { get; private set; }
#endif
        
        /// <summary>
        /// Contains id for current inquire. Id is incremented after release.
        /// </summary>
        private int inquireId { get; set; }

        private bool isInquired { get; set; }
        
        public InternalMicroOperation()
        {
            #if DEBUG
            ID = GetNextID();
            #endif
        }

        private void CancelAsyncDispose(int id)
        {
            if (id != inquireId)
            {
                Debug.LogError("Attempt to cancel async dispose with invalid id");
                return;
            }

            if (m_AsyncDisposeCancellationTokenSource != null)
            {
                /* If async dispose is called, we should cancel async dispose token and dispose it */
                m_AsyncDisposeCancellationTokenSource.Cancel();
                m_AsyncDisposeCancellationTokenSource.Dispose();
                m_AsyncDisposeCancellationTokenSource = null;
            }
        }

        internal int Inquire()
        {
            if (isInquired)
            {
                throw new InvalidOperationException("MicroOperation is already inquired");
            }
            isInquired = true;
            return inquireId;
        }
        
        internal async UniTask Release(int id, bool async = false, bool ignoreExceptions = false)
        {
            if (id != inquireId)
            {
                if (ignoreExceptions) return;
                throw new InvalidOperationException("Invalid inquire id");
            }
            isDisposed = true;
            OnDisposed?.Invoke();
            
            var dependenciesIsValid = dependencies != null;
            void CancelAsyncDisposeById()
            {
                isConcluded = false;
                CancelAsyncDispose(id);
            }
            
            if (dependenciesIsValid)
            {
                m_ExposedDisposedDependencies = new List<IExposedDisposed>();
                m_NotExposedDisposedDependencies = new List<object>();
                foreach (var d in dependencies)
                {
                    if (d == null || d.Equals(null))
                    {
                        dependenciesIsValid = false;
                        break;
                    }
                    if (d is IExposedDisposed exposedDisposed)
                    {
                        if (exposedDisposed.isDisposed)
                        {
                            dependenciesIsValid = false;
                            break;
                        }
                        m_ExposedDisposedDependencies.Add(exposedDisposed);
                    }
                    else
                    {
                        m_NotExposedDisposedDependencies.Add(d);
                    }
                }
                dependencies = null;
            }
            isConcluded = async && dependenciesIsValid;
            if (isConcluded)
            {
                m_AsyncDisposeCancellationTokenSource = new CancellationTokenSource();
                if (dependenciesIsValid)
                {
                    foreach (var dependency in m_ExposedDisposedDependencies)
                    {
                        dependency.OnDisposed += CancelAsyncDisposeById;
                    }
                    WatchDependencies(m_AsyncDisposeCancellationTokenSource.Token).Forget();
                }
            }
            
            m_PerformCancellationTokenSource.Cancel();
            m_PerformCancellationTokenSource.Dispose();
            m_PerformCancellationTokenSource = null;

            if (m_ExecuteTask.HasValue)
            {
                try
                {
                    await m_ExecuteTask.Value;
                }
                catch (OperationCanceledException)
                {
                    // ignored
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error was thrown during operation async disposing: {e}");
                }
            }

            if (m_ExposedDisposedDependencies != null)
            {
                foreach (var dependency in m_ExposedDisposedDependencies)
                {
                    if (dependency != null && !dependency.Equals(null))
                    {
                        dependency.OnDisposed -= CancelAsyncDisposeById;
                    }
                }
            }
            
            inquireId++;
            
            m_ExposedDisposedDependencies?.Clear();
            m_NotExposedDisposedDependencies?.Clear();
            m_AsyncDisposeCancellationTokenSource?.Cancel();
            m_AsyncDisposeCancellationTokenSource?.Dispose();
            m_AsyncDisposeCancellationTokenSource = null;
            m_ExecuteTask = null;
            dependencies = null;
            isConcluded = false;
            isInquired = false;
            #if DEBUG
            isDebug = false;
            #endif
            #if DEBUG
            _InstanceCount--;
            #endif
            Pool2.Return(this);
        }
        
        private async UniTask WatchDependencies(CancellationToken token)
        {
            while (token.IsCancellationRequested == false)
            {
                var isValid = ValidateDependencies();
                if (!isValid)
                {
                    CancelAsyncDispose(inquireId);
                }
                await UniTask.Yield(PlayerLoopTiming.EarlyUpdate);
            }
        }

        private bool ValidateDependencies()
        {
            foreach (var dependency in m_NotExposedDisposedDependencies)
            {
                if (dependency == null || dependency.Equals(null))
                {
                    return false;
                }
            }

            return true;
        }

        private void InternalRun(AsyncAction action)
        {
            if (m_ExecuteTask.HasValue)
            {
                throw new InvalidOperationException("Task is already running");
            }
            m_PerformCancellationTokenSource = new CancellationTokenSource();
            m_ExecuteTask = action(m_PerformCancellationTokenSource.Token);
        }
        
        private void InternalRun(AsyncOperationAction action)
        {
            if (m_ExecuteTask.HasValue)
            {
                throw new InvalidOperationException("Task is already running");
            }
            m_PerformCancellationTokenSource = new CancellationTokenSource();
            m_ExecuteTask = action(this);
        }

        public bool isDisposed { get; private set; }
        public event Action OnDisposed;
    }
}