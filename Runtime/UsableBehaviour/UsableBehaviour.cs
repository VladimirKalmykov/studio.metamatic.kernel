using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Metamatic
{
    [Obsolete("Use RunnableBehaviour instead", false)]
    public abstract class UsableBehaviour : MonoBehaviour, IDisposable, INotifyCompletion
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
            if (!IsCompleted)
            {
                throw new Exception("UsableBehaviour is not completed");
            }

            IsCompleted = false;
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
            StartActivity(m_CancellationTokenSource.Token);
            return this;
        }

        protected virtual async void StartActivity(CancellationToken token)
        {
            try
            {
                await Activity(token);
            }
            catch (OperationCanceledException)
            {
                // Ignore
                Debug.Log("Activity was cancelled");
                return;
            }
            finally
            {
                CompleteAndFinalize();
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
            CompleteAndRenew();
            m_DeactivationCancellationTokenSource = new CancellationTokenSource();
            m_DeactivationRequestTaskCompletionSource.SetResult(m_DeactivationCancellationTokenSource.Token);
        }
        
        /// <summary>
        /// Just complete task
        /// </summary>
        protected void Complete()
        {
            m_ActivityTaskCompletionSource?.TrySetResult(null);
            SetFinallized();
        }
        
        /// <summary>
        /// Complete task and start new
        /// </summary>
        protected void CompleteAndRenew()
        {
            Complete();
            IsCompleted = false;
            m_ActivityTaskCompletionSource = new TaskCompletionSource<object>();
        }
        
        /// <summary>
        /// Complete task and nullify
        /// </summary>
        protected void CompleteAndFinalize()
        {
            Complete();
            m_ActivityTaskCompletionSource = null;
        }

        #endregion
        
        #region Virtual Methods

        protected abstract Task Activity(CancellationToken cancellationToken);

        #endregion
        
        #region Crital Notify Completion
        // ReSharper disable once InconsistentNaming
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public bool IsCompleted { get; private set; } = true;
        public event Action OnComplete;
        // void ICriticalNotifyCompletion.UnsafeOnCompleted(Action continuation)
        // {
        //     OnComplete += continuation;
        // }
        //
        void INotifyCompletion.OnCompleted(Action continuation)
        {
            OnComplete += continuation;
        }
        
        private void SetFinallized()
        {
            IsCompleted = true;
            OnComplete?.Invoke();
            OnComplete = null;
        }
    
        public TaskAwaiter GetAwaiter()
        {
            return activityTask == null ? Task.CompletedTask.GetAwaiter() : activityTask.GetAwaiter();
        }
        #endregion
    }

    public abstract class UsableBehaviour<TProps> : UsableBehaviour
    where TProps : notnull
    {
        private TProps m_UsableProps;
        public UsableBehaviour Activate(in TProps props, CancellationToken cancellationToken = default)
        {
            this.m_UsableProps = props;
            return base.Activate(cancellationToken);
        }

        protected override async void StartActivity(CancellationToken token)
        {
            try
            {
                await Activity(m_UsableProps, token);
            }
            catch (OperationCanceledException)
            {
                // Ignore
                Debug.Log("Activity was cancelled");
                return;
            }
            finally
            {
                CompleteAndFinalize();
            }
        }
        protected abstract Task Activity(TProps props, CancellationToken cancellationToken);

        protected override sealed Task Activity(CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("This method should not be called in opposite to its generic version");
        }
    }
}