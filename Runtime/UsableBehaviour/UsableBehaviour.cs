using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Metamatic
{
    /// <summary>
    /// Allows to use behaviour component with operator usage
    /// </summary>
    public abstract class UsableBehaviour : MonoBehaviour, IDisposable
    {
        #region Serialized Fields

        
        #endregion
        
        #region Protected properties

        protected bool m_IsActivated = false;
        #endregion
        
        #region Usable

        private TaskCompletionSource<object> m_ActivityTaskCompletionSource;
        private TaskCompletionSource<CancellationToken> m_DeactivationRequestTaskCompletionSource;
        private CancellationTokenSource m_CancellationTokenSource; 
        private CancellationTokenSource m_DeactivationCancellationTokenSource; 

        public Task activityTask
        {
            get
            {
                return m_ActivityTaskCompletionSource?.Task;
            }
        }
        
        /// <summary>
        /// This task will be resolved on dispose call
        /// </summary>
        public Task<CancellationToken> deactivationRequest
        {
            get
            {
                return m_DeactivationRequestTaskCompletionSource.Task;
            }
        }
        public UsableBehaviour Activate(CancellationToken token = default)
        {
            if (m_IsActivated)
            {
                throw new Exception("UsableBehaviour has been activated by another call");
            }
            /* Cancel previous deactivation if exists */
            if (m_DeactivationCancellationTokenSource != null &&
                !m_DeactivationCancellationTokenSource.IsCancellationRequested)
            {
                m_DeactivationCancellationTokenSource?.Cancel();
            }

            m_CancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
            m_IsActivated = true;
            m_ActivityTaskCompletionSource?.SetResult(null);
            // throw new Exception("Expect activity task completion source to be a null");

            if (m_DeactivationRequestTaskCompletionSource != null && !m_DeactivationRequestTaskCompletionSource.Task.IsCompleted)
            {
                throw new Exception("Expect deactivation task to be completed");
            }

            m_DeactivationRequestTaskCompletionSource = new TaskCompletionSource<CancellationToken>();
            m_ActivityTaskCompletionSource = new TaskCompletionSource<object>();
            StartActivity();
            return this;
        }

        private async void StartActivity()
        {
            try
            {
                await Activity(m_CancellationTokenSource.Token);
            }
            catch (OperationCanceledException e)
            {
                // Ignore
                Debug.Log("Activity was cancelled");
                return;
            }
            finally
            {
                Complete();
            }
        }

        #endregion
        
        #region IDisposable
        
        
        public async void Dispose()
        {
            if (!m_IsActivated)
            {
                throw new Exception("You can not dispose unactivated usable behaviour. Use Activate() methods before.");
            }

            m_IsActivated = false;
            CompleteAndInitialize();
            m_DeactivationCancellationTokenSource = new CancellationTokenSource();
            m_DeactivationRequestTaskCompletionSource.SetResult(m_DeactivationCancellationTokenSource.Token);
        }

        private void Complete()
        {
            m_ActivityTaskCompletionSource?.TrySetResult(null);
            m_ActivityTaskCompletionSource = null;
            SetFinallized();
        }

        protected void CompleteAndInitialize()
        {
            Complete();
            m_ActivityTaskCompletionSource = new TaskCompletionSource<object>();
        }

        #endregion
        
        #region Virtual Methods

        protected abstract Task Activity(CancellationToken cancellationToken);

        #endregion
        
        #region Crital Notify Completion
        // ReSharper disable once InconsistentNaming
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        // public bool IsCompleted { get; private set; }
        // public event Action OnComplete;
        // void ICriticalNotifyCompletion.UnsafeOnCompleted(Action continuation)
        // {
        //     OnComplete += continuation;
        // }
        //
        // void INotifyCompletion.OnCompleted(Action continuation)
        // {
        //     OnComplete += continuation;
        // }
    
        private void SetFinallized()
        {
            // IsCompleted = true;
            // OnComplete?.Invoke();
            // OnComplete = null;
        }
    
        // public TaskAwaiter GetAwaiter()
        // {
        //     return activityTask == null ? Task.CompletedTask.GetAwaiter() : activityTask.GetAwaiter();
        // }
        #endregion
    }
}