using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic
{
    /// <summary>
    /// It provides a polymorphic functionality for showing and hiding a game object, the main feature of which is the ability to flexibly show and hide an object, as well as respond to the event of its final hiding.
    /// </summary>
    public abstract class DebugPresenter: MonoBehaviour, IAbortable, IResettable
    {
        public enum State
        {
            Idle,
            Presenting,
            Dismissing,
        }

        private SignalSource m_StepCancellationSignalSource = default;
        private SignalSource m_CompleteDismissSignalSource = default;
        public State state { get; private set; }

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
            state = State.Idle;
            m_CompleteDismissSignalSource.Dispose();
            m_CompleteDismissSignalSource = SignalSource.Create();
            OnReset();
        }
        
        /// <summary>
        /// Call this method to show the presenter.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public void Present()
        {
            /*
             * If the presenter is already presenting, do nothing.
             */
            if (state == State.Presenting)
            {
                return;
            }
            state = State.Presenting;
            m_StepCancellationSignalSource.TryRaise();
            m_StepCancellationSignalSource = SignalSource.Create();
            // OnPresent(m_StepCancellationSignalSource.Signal);
        }
        
        /// <summary>
        /// Call this method to hide the presenter.
        /// </summary>
        public void Dismiss()
        {
            if (state == State.Dismissing)
            {
                return;
            }
            // AsyncHide().Forget();
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
            return PlayerLoop.Delay(1f, PlayerLoopTiming.Update, cancellationSignal);
            //return UniTask.WhenAny(completeDismissSignal.ToUniTask(), cancellationSignal.ToUniTask());
        }

        private async UniTask AsyncHide()
        {
            state = State.Dismissing;
            m_StepCancellationSignalSource.TryRaise();
            m_StepCancellationSignalSource = SignalSource.Create();
            await UniTask.NextFrame();
            try
            {
                await OnDismiss(m_StepCancellationSignalSource.Signal);
                Reset();
            }
            catch (OperationCanceledException)
            {
                // ignored
            } catch (Exception e)
            {
                Console.Error("Presenter", e);
            }
        }
        
        public bool isAborted { get; private set; } = false;
        private event Action OnAborted;
        public void Abort()
        {
            isAborted = true;
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
    }
}