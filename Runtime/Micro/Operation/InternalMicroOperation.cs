using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Metamatic.DevTools;
using UnityEngine;

namespace Metamatic
{
    public class InternalMicroOperation : IExposedDisposed
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
        public CancellationToken asyncDisposeCancellationToken =>
            m_AsyncDisposeCancellationTokenSource?.Token ?? CancellationToken.None;

        /// <summary>
        /// Contains task that is returned by action. This task is used only for async dispose.
        /// </summary>
        private UniTask? m_ExecuteTask;

        /// <summary>
        /// Current operation is aborted
        /// </summary>
        private bool m_IsAborted;

        /// <summary>
        /// Determines if operation is concluding without aborting by dependency invalidation.
        /// </summary>
        public bool isConcluded { get; private set; } = true;

        /// <summary>
        /// Determines if operation is aborted by dependency invalidation.
        /// </summary>
        public bool isAborted => m_IsAborted;
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

            m_IsAborted = true;

            if (m_AsyncDisposeCancellationTokenSource == null)
            {
                m_AsyncDisposeCancellationTokenSource = new CancellationTokenSource();
            }

            /* If async dispose is called, we should cancel async dispose token and dispose it */
            m_AsyncDisposeCancellationTokenSource.Cancel();
            m_AsyncDisposeCancellationTokenSource.Dispose();
            m_AsyncDisposeCancellationTokenSource = null;

            /* Cancel perform token too */
            if (m_PerformCancellationTokenSource is { IsCancellationRequested: false })
            {
                m_PerformCancellationTokenSource.Cancel();
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


            isConcluded = async && !m_IsAborted;
            if (isConcluded)
            {
                m_AsyncDisposeCancellationTokenSource = new CancellationTokenSource();
            }

            if (!m_PerformCancellationTokenSource.IsCancellationRequested)
            {
                m_PerformCancellationTokenSource.Cancel();
            }

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

            inquireId++;

            m_AsyncDisposeCancellationTokenSource?.Dispose();
            m_AsyncDisposeCancellationTokenSource = null;
            m_ExecuteTask = null;
            dependencies = null;
            isConcluded = false;
            isInquired = false;
            m_IsAborted = false;
#if DEBUG
            isDebug = false;
#endif
#if DEBUG
            _InstanceCount--;
#endif
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
                    ObserveExposedDisposable.Create(dependencies, () => CancelAsyncDispose(currentId), true);
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

            var currentId = inquireId;
            if (dependencies != null)
            {
                OnBeforeReturn +=
                    ObserveExposedDisposable.Create(dependencies, () => CancelAsyncDispose(currentId), true);
            }

            m_PerformCancellationTokenSource = new CancellationTokenSource();
                m_ExecuteTask = action(this);
         }

        public bool isDisposed { get; private set; }
        public event Action OnDisposed;
    }
}