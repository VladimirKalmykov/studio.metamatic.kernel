using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Metamatic
{
    /// <summary>
    /// It provides a polymorphic functionality for showing and hiding a game object, the main feature of which is the ability to flexibly show and hide an object, as well as respond to the event of its final hiding.
    /// </summary>
    public abstract class Presenter : MonoBehaviour, IAbortable, IResettable
    {
        [SerializeField] [BoxGroup("Presenter")]
        private bool autoPresentOnEnable = false;

        [SerializeField] [BoxGroup("Presenter")]
        private bool deactivateOnDismiss = false;

        [SerializeField] [BoxGroup("Presenter")]
        public bool ignoreParentPresenter = false;
        
        [SerializeField] [BoxGroup("Presenter")]
        public bool ignoreChildrenPresenters = false;

        [SerializeField] [BoxGroup("Presenter")]
        public Presenter[] subPresenters = Array.Empty<Presenter>();

        public enum State
        {
            Idle,
            Presenting,
            Dismissing,
        }

        private SignalSource m_StepCancellationSignalSource = default;
        private SignalSource m_CompletePresentSignalSource = default;
        private SignalSource m_CompleteDismissSignalSource = default;
        private AsyncCancellationSource m_ActiveCancellationSource = default;
        private List<Presenter> m_ChildPresenters = new List<Presenter>();
        private bool m_IsStarted = false;
        public State state { get; private set; }

        protected virtual void Awake()
        {

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

        protected virtual void OnEnable()
        {
            if (autoPresentOnEnable)
            {
                Present();
            }
        }

        protected virtual void OnDisable()
        {
            if (state != State.Idle)
            {
                m_StepCancellationSignalSource.TryRaise();
                Reset();
            }

            state = State.Idle;
        }

        protected virtual void OnDestroy()
        {
            Abort();
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

        [BoxGroup("Presenter"), Button]
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
        [BoxGroup("Presenter"), Button]
        public virtual void Present(bool ignoreChildren = false)
        {
            /*
             * If the presenter is already presenting, do nothing.
             */
            if (state == State.Presenting)
            {
                return;
            }


            if (state == State.Idle)
            {
                state = State.Presenting;
                // First presentation
                enabled = true;
                gameObject.SetActive(true); // TODO: Cache gameObject?
                m_ActiveCancellationSource.Dispose();
                m_ActiveCancellationSource = AsyncCancellationSource.Create();
                OnActive(m_ActiveCancellationSource.Cancellation).Forget();
            }
            else
            {
                state = State.Presenting;
            }

            if (!ignoreChildrenPresenters && !ignoreChildren)
            {
                if (subPresenters != null && subPresenters.Length > 0)
                {
                    foreach (var neighborPresenter in subPresenters)
                    {
                        if (neighborPresenter.ignoreParentPresenter)
                        {
                            continue; // Skip neighbor presenters that ignore the parent presenter.
                        }

                        neighborPresenter.Present();
                    }
                }
                
#if UNITY_EDITOR
                if (Application.isPlaying)
                {
#endif
                    foreach (Transform child in transform)
                    {
                        GameObject go = child.gameObject;
                        if (go.hideFlags == HideFlags.None)
                        {
                            m_ChildPresenters.AddRange(child.GetComponentsInChildren<Presenter>(true));
                        }
                    }

#if UNITY_EDITOR
                }
#endif

                /*
                 * If the presenter has child presenters, show them as well.
                 */
                foreach (var childPresenter in m_ChildPresenters)
                {
                    if (childPresenter.ignoreParentPresenter)
                    {
                        continue; // Skip child presenters that ignore the parent presenter.
                    }

                    childPresenter.Present();
                }
            }

            if (!m_IsStarted) return; // Return if the presenter is not started yet.
            ContinuePreset();
        }

        public UniTask PresentAsync(Signal cancellationSignal)
        {
            Present();
            return WaitForPresent(cancellationSignal);
        }

        private void ContinuePreset()
        {
            m_StepCancellationSignalSource.TryRaise();
            var presentCancellationSource = m_StepCancellationSignalSource = SignalSource.Create();
            InternalAsyncPresent(presentCancellationSource.Signal).Forget();
        }

        private async UniTaskVoid InternalAsyncPresent(Signal signal)
        {
            try
            {
                m_CompletePresentSignalSource.TryRaise();
                m_CompletePresentSignalSource = SignalSource.Create();
                await OnPresent(signal);
            }
            catch (OperationCanceledException)
            {
                // Ignore
            }
            catch (Exception e)
            {
                Console.Error("Presenter", e);
            }
            finally
            {
                m_CompletePresentSignalSource.TryRaise();
            }
        }

        /// <summary>
        /// Call this method to hide the presenter.
        /// </summary>
        [BoxGroup("Presenter"), Button]
        public void Dismiss(bool ignoreChildren = false)
        {
            if (enabled == false || state != State.Presenting)
            {
                return;
            }

            AsyncHide().Forget();
            if (ignoreChildren || ignoreChildrenPresenters)
            {
                return;
            }

            if (subPresenters != null && subPresenters.Length > 0)
            {
                foreach (var neighborPresenter in subPresenters)
                {
                    if (neighborPresenter.ignoreParentPresenter)
                    {
                        continue; // Skip neighbor presenters that ignore the parent presenter.
                    }

                    neighborPresenter.Dismiss();
                }
            }

            foreach (var childPresenter in m_ChildPresenters)
            {
                if (childPresenter.ignoreParentPresenter)
                {
                    continue; // Skip child presenters that ignore the parent presenter.
                }

                childPresenter.Dismiss();
            }
        }

        public UniTask Dismiss(Signal cancellationSignal)
        {
            Dismiss();
            return WaitForDismissal(cancellationSignal);
        }

        public UniTask DismissAsync(Signal cancellationSignal = default)
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
            // Check state
            if (state == State.Idle)
            {
                return UniTask.CompletedTask;
            }
            
            return UniTask.WhenAny(completeDismissSignal.ToUniTask(), cancellationSignal.ToUniTask());
        }

        public UniTask WaitForPresent(Signal cancellationSignal)
        {
            if (m_CompletePresentSignalSource.IsRaised)
            {
                return UniTask.CompletedTask;
            }
            
            return UniTask.WhenAny(m_CompletePresentSignalSource.Signal.ToUniTask(), cancellationSignal.ToUniTask());
        }

        private async UniTask AsyncHide()
        {
            state = State.Dismissing;
            m_StepCancellationSignalSource.TryRaise();
            var dismissStepCancellationSource = m_StepCancellationSignalSource = SignalSource.Create();
            var scene = gameObject.scene;
            
            await UniTask.NextFrame();
            if (dismissStepCancellationSource.IsRaised) return;
            try
            {
                SceneManager.sceneUnloaded += OnObjectSceneUnloaded;
                var dismissCancellationSignal = dismissStepCancellationSource.Signal;
                await OnDismiss(dismissCancellationSignal);
                if (!dismissCancellationSignal.isRaised)
                {
                    Reset();
                    if (deactivateOnDismiss)
                    {
                        gameObject.SetActive(false);
                    }
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
            finally
            {
                SceneManager.sceneUnloaded -= OnObjectSceneUnloaded;
            }
        }

        private void OnObjectSceneUnloaded(Scene scene)
        {
            m_StepCancellationSignalSource.TryRaise();
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
        protected abstract UniTask OnPresent(Signal cancellationSignal);

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

        protected virtual UniTask OnPrepare(Action ready, Signal onDismiss)
        {
            ready();
            return UniTask.CompletedTask;           
        }

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