using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic
{
    public abstract class WidgetLegacy: MonoBehaviour
    {
        protected enum State
        {
            Hiding,
            Shown
        }
        private CancellationTokenSource m_ShowOperationCancellationTokenSource;
        private CancellationTokenSource m_HideOperationCancellationTokenSource;
        public CancellationToken showOperationCancellationToken => m_ShowOperationCancellationTokenSource.Token;

        public CancellationToken hideOperationCancellationToken =>
            m_HideOperationCancellationTokenSource?.Token ?? CancellationToken.None;
        private readonly Observable<bool> m_Busy = new Observable<bool>(false);
        public ObservableReadOnly<bool> busy => m_Busy;
        protected State state { get; set; } = State.Hiding;
        protected virtual void OnEnable()
        {
            BeforeShow();
        }

        protected virtual void OnDisable()
        {
            CancelShowOperation();
            CancelHideOperation();
            Invoke("DelayedReset", 0.1f);
        }
        
        private void DelayedReset()
        {
            if (this.enabled) return;
            SetIdle();
            state = State.Hiding;
        }

        protected virtual void OnDestroy()
        {
            
        }

        protected virtual void SetBusy()
        {
            busy.value = true;
        }
        
        protected virtual void SetIdle()
        {
            busy.value = false;
        }
        
        protected void CancelShowOperation()
        {
            m_ShowOperationCancellationTokenSource?.Cancel();
        }
        
        protected void CancelHideOperation()
        {
            m_HideOperationCancellationTokenSource?.Cancel();
        }
        
        protected void RenewShowOperation()
        {
            CancelShowOperation();
            m_ShowOperationCancellationTokenSource = new CancellationTokenSource();
        }
        
        protected void RenewHideOperation()
        {
            CancelHideOperation();
            m_HideOperationCancellationTokenSource = new CancellationTokenSource();
        }
        
        protected void ClearShowOperation()
        {
            m_ShowOperationCancellationTokenSource = null;
        }
        
        protected void ClearHideOperation()
        {
            m_HideOperationCancellationTokenSource = null;
        }
        
        public async UniTask Show(CancellationToken cancellationToken = default)
        {
           
            CancellationTokenRegistration registration = default;
            
            if (state == State.Shown)
            {
                Debug.LogWarning("Already shown");
                return;
            }
            SetBusy();
            CancelHideOperation();
            RenewShowOperation();
            var combinedToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, showOperationCancellationToken).Token;
            registration = combinedToken.Register(() =>
            {
                registration.Dispose();
                Hide().Forget();
            });
            state = State.Shown;
            BeforeShow();
            gameObject.SetActive(true);
            try
            {
                await PerformShow(combinedToken);
                combinedToken.ThrowIfCancellationRequested();
                ClearShowOperation();
                AfterShow();
            } catch (OperationCanceledException)
            {
                ClearShowOperation();
            }
        }
        
        public async UniTask Hide()
        {
            if (state == State.Hiding)
            {
                Debug.LogWarning("Already hiding");
                return;
            }
            CancelShowOperation();
            RenewHideOperation();
            state = State.Hiding;
            BeforeHide();
            
            try
            {
                await PerformHide(hideOperationCancellationToken);
                if (hideOperationCancellationToken.IsCancellationRequested)
                {
                    throw new OperationCanceledException();
                }
                ClearHideOperation();
                AfterHide();
                
                gameObject.SetActive(false);
                SetIdle();
            }
            catch (OperationCanceledException)
            {
                ClearHideOperation();
            }
        }

        protected virtual void BeforeShow() {}

        protected abstract UniTask PerformShow(CancellationToken cancellationToken = default);

        protected virtual void AfterShow() {}

        protected virtual void BeforeHide()
        {
        }

        protected abstract UniTask PerformHide(CancellationToken cancellationToken = default);

        protected virtual void AfterHide()
        {
        }
    }
}