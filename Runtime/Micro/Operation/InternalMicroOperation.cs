using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Metamatic.DevTools;
using UnityEngine;
using UnityEngine.Serialization;

namespace Metamatic
{
    public class InternalMicroOperation
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

        public static MicroOperation Call(AsyncAction action)
        {
            var operation = Pool2.Get();

#if DEBUG
            operation.debugUseCount++;
            _InstanceCount++;
#endif

            operation.InternalRun(action);
            return new MicroOperation(operation);
        }
        
        public static MicroOperation Run(AsyncOperationAction action)
        {
            var operation = Pool2.Get();

#if DEBUG
            operation.debugUseCount++;
            _InstanceCount++;
#endif

            operation.InternalRun(action);
            return new MicroOperation(operation);
        }

        private CancellationTokenSource m_DisposeCancellationTokenSource;
        public CancellationToken cancellationToken => m_DisposeCancellationTokenSource.Token;
        /// <summary>
        /// Contains task that is returned by action. This task is used only for async dispose.
        /// </summary>
        private UniTask? m_ExecuteTask;
        /// <summary>
        /// Determines if Dispose was called synchronously or asynchronously. User can call Dispose or DisposeAsync. m_DisposeCalledSynchronously is set to true if Dispose is called.
        /// User within execution can check this value to determine if Dispose was called synchronously or asynchronously. And if it was called synchronously, user should not perform any async operations
        /// on operation completion.
        /// </summary>
        public bool shouldDisposedAsync { get; private set; }
        
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
        
        ~InternalMicroOperation()
        {
            Dispose();
        }

        public UniTask<bool> Completion()
        {
            var uniTaskSource = new UniTaskCompletionSource<bool>();
            m_DisposeCancellationTokenSource.Token.Register(() => uniTaskSource.TrySetResult(shouldDisposedAsync));
            return uniTaskSource.Task;
        }

        private void Dispose()
        {
            m_DisposeCancellationTokenSource?.Cancel();
            m_DisposeCancellationTokenSource?.Dispose();
            m_DisposeCancellationTokenSource = null;
            GC.SuppressFinalize(this);
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
            
            shouldDisposedAsync = async;
            inquireId++;
            
            m_DisposeCancellationTokenSource.Cancel();
            m_DisposeCancellationTokenSource.Dispose();
            m_DisposeCancellationTokenSource = null;

            if (m_ExecuteTask.HasValue)
            {
                try
                {
                    await m_ExecuteTask.Value;
                } catch (OperationCanceledException)
                {
                    // ignored
                }
                
                m_ExecuteTask = null;
            }

            shouldDisposedAsync = false;
            isInquired = false;
            _InstanceCount--;
            Pool2.Return(this);
        }

        private void InternalRun(AsyncAction action)
        {
            if (m_ExecuteTask.HasValue)
            {
                throw new InvalidOperationException("Task is already running");
            }
            m_DisposeCancellationTokenSource = new CancellationTokenSource();
            m_ExecuteTask = action(m_DisposeCancellationTokenSource.Token);
        }
        
        private void InternalRun(AsyncOperationAction action)
        {
            if (m_ExecuteTask.HasValue)
            {
                throw new InvalidOperationException("Task is already running");
            }
            m_DisposeCancellationTokenSource = new CancellationTokenSource();
            m_ExecuteTask = action(this);
        }
    }
}