using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace Metamatic.EditorTools
{
    public abstract class AsyncVisualElementLegacy: VisualElement
    {
        public enum AsyncStatus
        {
            Pending,
            Initialized,
            Failed,
            Cancelled,
            Finished
        }
        public AsyncStatus asyncStatus { get; private set; }
        public Exception initializationException { get; private set; }
        public event Action OnInitialized;

        // ReSharper disable once InconsistentNaming
        public UniTask ReadyTask
        {
            get
            {
                if (asyncStatus == AsyncStatus.Initialized || asyncStatus == AsyncStatus.Finished)
                {
                    return UniTask.CompletedTask;
                }
                else
                {
                    var tcs = SafeUniTaskCompletionSource.Create();
                    OnInitialized += () =>
                    {
                        if (asyncStatus == AsyncStatus.Cancelled)
                        {
                            tcs.TrySetCanceled();
                        }
                        if (asyncStatus == AsyncStatus.Failed)
                        {
                            tcs.TrySetException(initializationException);
                        }
                        else
                        {
                            tcs.TrySetResult();
                        }
                    };
                    return tcs.Task;
                }
            }
        }

        private CancellationSignal m_CancellationSignal;

        private UniTask ReadyAsync()
        {
            asyncStatus = AsyncStatus.Initialized;
            OnInitialized?.Invoke();
            return m_CancellationSignal.CanBeCanceled ? m_CancellationSignal.ToUniTask() : UniTask.CompletedTask;
        }
        
        public AsyncVisualElementLegacy(CancellationSignal signal): base()
        {
            m_CancellationSignal = signal;
            InternalInitializeAsync().Forget();
        }
        
        private async UniTaskVoid InternalInitializeAsync()
        {
            try
            {
                asyncStatus = AsyncStatus.Pending;
                await InitializeAsync(ReadyAsync, m_CancellationSignal);
                asyncStatus = AsyncStatus.Finished;
                OnInitialized?.Invoke();
            }
            catch (OperationCanceledException)
            {
                asyncStatus = AsyncStatus.Cancelled;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                asyncStatus = AsyncStatus.Failed;
            }
        }
        
        protected abstract UniTask InitializeAsync(Func<UniTask> ready, CancellationSignal signal);
    }
}