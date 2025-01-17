using System;
using Cysharp.Threading.Tasks;
using Metamatic.DevTools;
using UnityEngine;

namespace Metamatic
{
    public class InternalMicroOperation : IExposedAsyncCancellation, IAbortable
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
            Monitoring.Create("MicroOperation", "Instances count", () => _InstanceCount);
        }
#endif
        public delegate UniTask AsyncAction(Signal signal);

        public delegate UniTask AsyncOperationAction(InternalMicroOperation operation);

        private static readonly ParamLessPool<InternalMicroOperation> Pool2 = new(20);

        /// <summary>
        /// This event is called before the operation is returned to the pool.
        /// </summary>
        private event Action OnBeforeReturn;

        public static MicroOperation Call(AsyncAction action, object[] dependencies = null)
        {
            var operation = Pool2.Get();
#if DEBUG
            operation.debugUseCount++;
            _InstanceCount++;
#endif
            operation.dependencies = dependencies;
            operation.isDisposedSync = false;

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
            operation.isDisposedSync = false;

            operation.InternalRun(action);
            return new MicroOperation(operation);
        }

        /// <summary>
        /// Contains dependencies for the current operation. When it disposed all dependencies should not be null to dispose async.
        /// </summary>
        // ReSharper disable once InconsistentNaming
        public object[] dependencies { get; private set; }

        private SignalSource m_PerformCancellationTokenSource;

        /// <summary>
        /// This token is used to cancel async dispose. It is created when DisposeAsync is called.
        /// </summary>
        private SignalSource m_AsyncDisposeCancellationTokenSource;

        public Signal cancellationSignal => m_PerformCancellationTokenSource.Signal;

        /// <summary>
        /// CancellationToken for async dispose. It will cancel async dispose.
        /// </summary>
        public Signal asyncDisposeCancellationToken =>
            m_AsyncDisposeCancellationTokenSource.Signal;

        /// <summary>
        /// Contains task that is returned by action. This task is used only for async dispose.
        /// </summary>
        private UniTask? m_ExecuteTask;

        /// <summary>
        /// Current operation is aborted
        /// </summary>
        private bool m_IsAborted;

        /// <summary>
        /// Determines if operation is aborted by dependency invalidation.
        /// </summary>
        public bool isAborted => m_IsAborted;

        public event Action OnAborted;
        public void Abort()
        {
            Abort(inquireId);
        }

        public DisposableAction AddAbortedListenerOnce(Action action)
        {
            OnAborted += action;
            return DisposableAction.Create(() => OnAborted -= action);
        }
#if DEBUG
        public bool isDebug { get; private set; }
#endif

        /// <summary>
        /// Contains id for current inquire. Id is incremented after release.
        /// </summary>
        public int inquireId { get; private set; } = 1;

        private bool isInquired { get; set; }

        public InternalMicroOperation()
        {
#if DEBUG
            ID = GetNextID();
#endif
        }

        public void Abort(int id)
        {
            if (id != inquireId)
            {
                Debug.LogError("Attempt to cancel async dispose with invalid id");
                return;
            }

            m_IsAborted = true;
            OnAborted?.Invoke();
            
            /* Mark as disposed sync */
            isDisposedSync = true;
            OnDisposedSync?.Invoke();
            /* Cancel perform token too */
            m_PerformCancellationTokenSource.TryRaise();
            
            /* If async dispose is called, we should cancel async dispose token and dispose it */
            m_AsyncDisposeCancellationTokenSource.TryRaise();
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

        internal async UniTask Release(int id, bool ignoreExceptions = false)
        {
            if (id != inquireId)
            {
                if (ignoreExceptions) return;
                throw new InvalidOperationException("Invalid inquire id");
            }

            isDisposedSync = true;
            OnDisposedSync?.Invoke();

            m_PerformCancellationTokenSource.TryRaise();

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
                    isDisposedAsync = true;
                    OnDisposedAsync?.Invoke(e);
                    Debug.LogError($"Error was thrown during operation async disposing: {e}");
                }
            }

            if (!isDisposedAsync)
            {
                isDisposedAsync = true;
                OnDisposedAsync?.Invoke(null);
            }

            unchecked
            {
                inquireId++;
            }
            
            OnDisposedAsync = null;
            m_ExecuteTask = null;
            dependencies = null;
            isInquired = false;
            m_IsAborted = false;
#if DEBUG
            isDebug = false;
            _InstanceCount--;
#endif
            
            isDisposedAsync = false;
            OnBeforeReturn?.Invoke();
            OnBeforeReturn = null;
            Pool2.Return(this);
        }

        private void InternalRun(AsyncAction action)
        {
            if (m_ExecuteTask.HasValue)
            {
                throw new InvalidOperationException("Task is already running");
            }

            var currentId = inquireId;
            if (dependencies != null)
            {
                OnBeforeReturn +=
                    ObserveExposedDisposable.Create(dependencies, () =>
                    {
                        Abort(currentId);
                    }, true);
            }

            m_PerformCancellationTokenSource = SignalSource.Create();
            m_AsyncDisposeCancellationTokenSource = SignalSource.Create();
            m_ExecuteTask = action(m_PerformCancellationTokenSource.Signal);
        }

        private void InternalRun(AsyncOperationAction action)
        {
            if (m_ExecuteTask.HasValue)
            {
                throw new InvalidOperationException("Task is already running");
            }

            var currentId = inquireId;
            if (dependencies != null)
            {
                OnBeforeReturn +=
                    ObserveExposedDisposable.Create(dependencies, () =>
                    {
                        Abort(currentId);
                    }, true);
            }

            m_PerformCancellationTokenSource = SignalSource.Create();
            m_AsyncDisposeCancellationTokenSource = SignalSource.Create();
                m_ExecuteTask = action(this);
         }

        public bool isDisposedSync { get; private set; }
        public bool isDisposedAsync { get; private set; }
        public event Action OnDisposedSync;
        public event Action<Exception> OnDisposedAsync;
    }
}