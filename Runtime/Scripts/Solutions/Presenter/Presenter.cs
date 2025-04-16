using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic
{
    /// <summary>
    /// It provides a polymorphic functionality for showing and hiding a game object, the main feature of which is the ability to flexibly show and hide an object, as well as respond to the event of its final hiding.
    /// </summary>
    public abstract class Presenter: MonoBehaviour, IAbortable, IResettable
    {
        public enum State
        {
            Idle,
            Presenting,
            Dismissing,
        }

        private SignalSource m_StepCancellationSignalSource = default;
        private SignalSource m_CompleteDismissSignalSource = default;
        private AsyncCancellationSource m_ActiveCancellationSource = default;
        private List<Presenter> m_ChildPresenters = new List<Presenter>();
        private bool m_IsStarted = false;
        public State state { get; private set; }

        protected virtual void Awake()
        {
            if (Application.isPlaying)
            {
                m_ChildPresenters.AddRange(GetComponentsInChildren<Presenter>(true));
            }
        }

        protected virtual void Start()
        {
            m_IsStarted = true;
            OnReset();
            if (state == State.Presenting)
            {
                /*
                 * If the presenter is already presenting, continue the presentation.
                 */
                ContinuePreset();
            }
            else
            {
                OnReset();
            }
        }

        /// <summary>
        /// This signal is raised when the presenter is completely dismissed.
        /// </summary>
        public Signal completeDismissSignal
        {
            get
            {
                if (!m_CompleteDismissSignalSource.isAlive)
                {
                    m_CompleteDismissSignalSource = SignalSource.Create();
                }
                return m_CompleteDismissSignalSource.Signal;
            }
        }
        
        public void Reset()
        {
            if (Application.isPlaying)
            {
                state = State.Idle;
                m_ActiveCancellationSource.Dispose();
                m_CompleteDismissSignalSource.Dispose();
                m_CompleteDismissSignalSource = SignalSource.Create();
                OnReset();
            }
        }
        
        /// <summary>
        /// Call this method to show the presenter.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public void Present(bool ignoreChildren = false)
        {
            /*
             * If the presenter is already presenting, do nothing.
             */
            if (enabled == false || state == State.Presenting)
            {
                return;
            }

            if (state == State.Idle)
            {
                state = State.Presenting;
                // First presentation
                m_ActiveCancellationSource.Dispose();
                m_ActiveCancellationSource = AsyncCancellationSource.Create();
                OnActive(m_ActiveCancellationSource.Cancellation).Forget();
            }
            else
            {
                state = State.Presenting;
            }
            
            if (!ignoreChildren)
            {
                /*
                 * If the presenter has child presenters, show them as well.
                 */
                foreach (var childPresenter in m_ChildPresenters)
                {
                    childPresenter.Present(true);
                }
            }
            if (!m_IsStarted) return; // Return if the presenter is not started yet.
            ContinuePreset();
        }
        
        private void ContinuePreset()
        {
            m_StepCancellationSignalSource.TryRaise();
            var presentCancellationSource = m_StepCancellationSignalSource = SignalSource.Create();
            OnPresent(presentCancellationSource.Signal).Forget();
        }
        
        /// <summary>
        /// Call this method to hide the presenter.
        /// </summary>
        public void Dismiss(bool ignoreChildren = false)
        {
            if (enabled == false || state == State.Dismissing)
            {
                return;
            }
            AsyncHide().Forget();
            if (ignoreChildren)
            {
                return;
            }
            foreach (var childPresenter in m_ChildPresenters)
            {
                childPresenter.Dismiss(true);
            }
        }

        public UniTask Dismiss(Signal cancellationSignal)
        {
            Dismiss();
            return WaitForDismissal(cancellationSignal);
        }
        
        public void Set(bool isPresented)
        {
            if (isPresented)
            {
                Present();
            }
            else
            {
                Dismiss();
            }
        }
        
        /// <summary>
        /// Wait for the presenter to be dismissed.
        /// </summary>
        /// <returns></returns>
        public UniTask WaitForDismissal(Signal cancellationSignal)
        {
            return UniTask.WhenAny(completeDismissSignal.ToUniTask(), cancellationSignal.ToUniTask());
        }

        private async UniTask AsyncHide()
        {
            state = State.Dismissing;
            m_StepCancellationSignalSource.TryRaise();
            var dismissStepCancellationSource = m_StepCancellationSignalSource = SignalSource.Create();
            await UniTask.NextFrame();
            if (dismissStepCancellationSource.IsRaised) return;
            try
            {
                var dismissCancellationSignal = dismissStepCancellationSource.Signal;
                await OnDismiss(dismissCancellationSignal);
                if (!dismissCancellationSignal.isRaised)
                {
                    Reset();
                }
            }
            catch (OperationCanceledException)
            {
                // ignored
            }
            catch (Exception e)
            {
                Console.Error("Presenter", e);
            }
        }
        
        public bool isAborted { get; private set; } = false;
        private event Action OnAborted;
        public void Abort()
        {
            isAborted = true;
            m_ActiveCancellationSource.Abort();
            /*
             * Call step cancellation signal source to cancel the current step. It cancels both the presentation operation and the dismissal operation. 
             */
            m_StepCancellationSignalSource.TryRaise();
            OnAborted?.Invoke();
            /*
             * Call the abort method to immediately complete all animations and hide the UI element.
             */
            OnReset();
            /*
             * After the abort method is called, the presenter is in the dismissed state.
             */
            m_CompleteDismissSignalSource.TryRaise();
        }

        public DisposableAction AddAbortedListenerOnce(Action action)
        {
            OnAborted += action;
            return new DisposableAction(() => OnAborted -= action);
        }
        
        /// <summary>
        /// Async method to present the presenter. E.g. show a UI element via animation.
        /// </summary>
        /// <param name="cancellationSignal"></param>
        /// <returns></returns>
        protected abstract UniTaskVoid OnPresent(Signal cancellationSignal);
        /// <summary>
        /// Async method to dismiss the presenter. E.g. hide a UI element via animation.
        /// </summary>
        /// <param name="cancellationSignal"></param>
        /// <returns></returns>
        protected abstract UniTask OnDismiss(Signal cancellationSignal);
        /// <summary>
        /// Called when the presenter is aborted or manually reset. Should immediately stop all animations and hide the UI element.
        /// </summary>
        protected abstract void OnReset();
        /// <summary>
        /// Override this method if you need to perform some async actions while the presenter is active.
        /// </summary>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        protected virtual UniTaskVoid OnActive(AsyncCancellation cancellation)
        {
            return new UniTaskVoid();
        }
    }
}