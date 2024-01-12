using System;
using System.Threading;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Metamatic.UI
{
    public class UIWidget: MonoBehaviour
    {
        protected enum State
        {
            Hiding,
            Shown
        }
        private CancellationTokenSource m_ShowOperationCancellationTokenSource;
        private CancellationTokenSource m_HideOperationCancellationTokenSource;
        public CancellationToken showOperationCancellationToken => m_ShowOperationCancellationTokenSource.Token;
        public CancellationToken hideOperationCancellationToken => m_HideOperationCancellationTokenSource.Token;
        private readonly ObservableWritableValue<bool> m_Busy = new ObservableWritableValue<bool>(false);
        public ObservableValue<bool> busy => m_Busy;
        protected State state { get; set; } = State.Hiding;
        protected virtual void OnEnable()
        {
            
        }

        protected virtual void OnDisable()
        {
            m_ShowOperationCancellationTokenSource?.Cancel();
            m_HideOperationCancellationTokenSource?.Cancel();
        }

        protected virtual void OnDestroy()
        {
            m_ShowOperationCancellationTokenSource?.Cancel();
            m_HideOperationCancellationTokenSource?.Cancel();
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

        public void Show()
        {
            if (state == State.Shown)
            {
                Debug.LogWarning("Already shown");
                return;
            }
            SetBusy();
            CancelHideOperation();
            RenewShowOperation();
            state = State.Shown;
            BeforeShow();
            gameObject.SetActive(true);
            try
            {
                PerformShow(() =>
                {
                    if (showOperationCancellationToken.IsCancellationRequested)
                    {
                        return;
                    }
                    ClearShowOperation();
                    AfterShow();
                });
            } catch (OperationCanceledException)
            {
                ClearShowOperation();
            }
        }

        public void Hide()
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
                PerformHide(() =>
                {
                    if (hideOperationCancellationToken.IsCancellationRequested)
                    {
                        return;
                    }
                    ClearHideOperation();
                    AfterHide();
                
                    gameObject.SetActive(false);
                    SetIdle();
                });
            }
            catch (OperationCanceledException)
            {
                ClearHideOperation();
            }
        }

        protected virtual void BeforeShow()
        {
            
        }
        
        protected virtual void PerformShow(Action onEnd = null)
        {
            
        }

        protected virtual void AfterShow()
        {
            
        }
        
        protected virtual void BeforeHide()
        {
            
        }

        protected virtual void PerformHide(Action onEnd = null)
        {
            
        }
        
        protected virtual void AfterHide()
        {
            
        }
    }
}