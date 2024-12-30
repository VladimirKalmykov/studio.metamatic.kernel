using System;
using Cysharp.Threading.Tasks;
using Metamatic;
using Console = Metamatic.Console;

namespace Metamatic
{
    public readonly struct AsyncPresentation
    {
        
    }
    public abstract class AsyncPresenterSkatch: IPolyDisposable, IAbortable
    {
        private enum State
        {
            Idle,
            ActivatingPlacement,
            PlacementActivated,
            Instantiated,
            FadingIn,
            FadedIn,
            Processing,
            FadingOut,
            FadedOut,
            Released,
        }
        private readonly IPlacementProvider m_PlacementProvider;
        /// <summary>
        /// Determines that the presenter should be enabled (it is not guaranteed that the presenter is enabled right now, but it should).
        /// </summary>
        private bool m_EnableRequested;
        /// <summary>
        /// Complete activation signal is raised when the presenter is completely disabled. It creates every time when the presenter is enter enabling state.
        /// Initialized placement can not finish its work until the complete disabling signal is raised (except the case when the presenter is disposed or placement provider is disposed).
        /// </summary>
        private SignalSource m_CompleteActivationSignal = default;
        /// <summary>
        /// This cancellation signal source is used to cancel the current process of enabling/disabling. It is raised when the presenter's enable status is changed during the process.
        /// </summary>
        private SignalSource m_CurrentProcessCancellationSignalSource = default;

        private AsyncCancellationSource m_ActivationAsyncCancellationSource = default;
        private SafeUniTaskCompletionSource m_ActivationCompletionSource = default;
        private State m_State = State.Idle;
        
        public AsyncPresenterSkatch(IPlacementProvider placementProvider)
        {
            m_PlacementProvider = placementProvider;
        }

        private async UniTaskVoid Activate()
        {
            try
            {
                m_State = State.ActivatingPlacement;
                var withAbortable = AbortableObserver.CreateProxy(this);
                await using var placementRequest =
                    withAbortable(m_PlacementProvider.RequestPlacement(OnPlacementReady));
                /*
                 * Requested placement will wait until the presenter is completely disabled.
                 */
                await m_CompleteActivationSignal.Signal;
                m_State = State.Idle;
            }
            catch (OperationCanceledException)
            {
                // Skip
            }
            catch (Exception e)
            {
                Console.Error("AsyncPresenter", e);
            }
            finally
            {
                m_State = State.Idle;
            }
        }
        
        private async UniTask OnPlacementReady(IPositioningSupply positioningSupply, AsyncCancellation cancellation)
        {
            try
            {
                m_State = State.PlacementActivated;
                using var abortOnPlacementAbort = AbortableObserver.Observe(cancellation,
                    () =>
                    {
                        m_CurrentProcessCancellationSignalSource.TryRaise();
                        m_ActivationAsyncCancellationSource.TryCancelSync();
                    });
                using var cancellationListener = cancellation.Register((isAsync) =>
                {
                    if (m_EnableRequested)
                    {
                        m_CurrentProcessCancellationSignalSource.TryRaise();
                    }
                });

                while (!cancellation.isAborted && !m_CompleteActivationSignal.IsRaised)
                {
                    var shouldBeEnabled = m_EnableRequested && !cancellation.isCancelledAsync;
                    if (shouldBeEnabled)
                    {
                        var enableCancellationSignal = cancellation.isCancelledAsync
                            ? Signal.Raised
                            : m_CurrentProcessCancellationSignalSource.Signal;
                        switch (m_State)
                        {
                            case State.PlacementActivated:
                                try
                                {
                                    await OnInstantiate(enableCancellationSignal);
                                    m_State = State.Instantiated;
                                }
                                catch (OperationCanceledException)
                                {
                                    break;
                                }

                                break;
                            case State.Instantiated:
                            case State.FadingIn:
                            case State.FadingOut:
                                try
                                {
                                    m_State = State.FadingIn;
                                    m_ActivationAsyncCancellationSource = AsyncCancellationSource.Create();
                                    TryActivate(positioningSupply, m_ActivationAsyncCancellationSource.Cancellation).Forget();
                                    await OnFadeIn(enableCancellationSignal);
                                    m_State = State.FadedIn;
                                }
                                catch (OperationCanceledException)
                                {
                                    break;
                                }

                                break;
                            default:
                                throw new ArgumentOutOfRangeException($"State {m_State} is not supported here");
                        }
                    }
                    else
                    {
                        var disableCancellationSignal = cancellation.isCancelledAsync
                            ? cancellation.AbortSignal
                            : m_CurrentProcessCancellationSignalSource.Signal;
                        switch (m_State)
                        {
                            case State.FadingIn: // Present can be in FadingIn state when it was cancelled during fade in
                            case State.FadedIn:
                            case State.FadingOut:
                                try
                                {
                                    m_State = State.FadingOut;
                                    await OnFadeOut(disableCancellationSignal);
                                    m_State = State.FadedOut;
                                    // Here we need to wait activation to finish its work
                                    m_ActivationAsyncCancellationSource.TryCancelAsync();
                                    await m_ActivationCompletionSource.Task;
                                    await OnRelease(cancellation.AbortSignal);
                                }
                                catch (OperationCanceledException)
                                {
                                    break;
                                }

                                break;
                            case State.Instantiated:
                                try
                                {
                                    await OnRelease(disableCancellationSignal);
                                    m_State = State.Released;
                                    m_CompleteActivationSignal.TryRaise();
                                }
                                catch (OperationCanceledException)
                                {
                                    break;
                                }

                                break;
                            case State.Idle:
                                m_State = State.Released;
                                m_CompleteActivationSignal.TryRaise();
                                break;
                            default:
                                throw new ArgumentOutOfRangeException($"State {m_State} is not supported here");
                        }
                    }
                }
            }
            finally
            {
                m_ActivationAsyncCancellationSource.Dispose();
                if (m_State != State.Released)
                {
                    m_CompleteActivationSignal.TryRaise();
                    // If the presenter was not released, it should be terminated right now
                    Terminate();
                }
            }
        }

        private bool m_IsBeingActivated;
        private async UniTaskVoid TryActivate(IPositioningSupply positioningSupply, AsyncCancellation cancellation)
        {
            if (m_IsBeingActivated) return;
            try
            {
                m_ActivationCompletionSource = new SafeUniTaskCompletionSource();
                m_IsBeingActivated = true;
                await OnActive(positioningSupply, cancellation);
            }
            catch (OperationCanceledException)
            {
                // Skip
            }
            catch (Exception e)
            {
                Console.Error("AsyncPresenter", e);
            }
            finally
            {
                m_ActivationCompletionSource.TrySetResult();
                m_IsBeingActivated = true;
            }
        }
        
        /// <summary>
        /// Enable presenter. After enabling, the presenter will be activated and processed.
        /// You call safe call Enable() or Disable() multiple times, the presenter is smart enough to be enabled or disabled only once.
        /// </summary>
        public void Enable()
        {
            if (m_EnableRequested) return;
            m_EnableRequested = true;
            m_CurrentProcessCancellationSignalSource.TryRaise();
            m_CurrentProcessCancellationSignalSource = SignalSource.Create();
            if (!m_CompleteActivationSignal.isAlive)
            {
                m_CompleteActivationSignal = SignalSource.Create();
                Activate().Forget();
            }
        }

        /// <summary>
        /// Disable presenter. After disabling, the presenter will be faded out and released.
        /// You call safe call Enable() or Disable() multiple times, the presenter is smart enough to be enabled or disabled only once.
        /// </summary>
        public void Disable()
        {
            if (!m_EnableRequested) return;
            m_EnableRequested = false;
            m_CurrentProcessCancellationSignalSource.TryRaise();
            m_CurrentProcessCancellationSignalSource = SignalSource.Create();
        }

        public void SetEnabled(bool enabled)
        {
            if (enabled)
            {
                Enable();
            }
            else
            {
                Disable();
            }
        }

        public void Dispose()
        {
            Abort();
        }

        public UniTask DisposeAsync()
        {
            throw new System.NotImplementedException();
        }

        private event Action OnAbort;
        public bool isAborted { get; private set; }
        public void Abort()
        {
            isAborted = true;
            OnAbort?.Invoke();
            m_CurrentProcessCancellationSignalSource.Dispose();
        }

        public DisposableAction AddAbortedListenerOnce(Action action)
        {
            OnAbort += action;
            return new DisposableAction(() => OnAbort -= action);
        }
        
        #region Abstarct Methods

        /// <summary>
        /// Allows asynchronous instantiation of the presenter. This method is called when the presenter is ready.
        /// </summary>
        /// <param name="cancellationSignal"></param>
        /// <returns></returns>
        protected abstract UniTask OnInstantiate(Signal cancellationSignal);
        protected abstract UniTask OnFadeIn(Signal cancellationSignal);
        /// <summary>
        /// OnActivate is called when the presenter is ready to be active. Its lifecycle is lying between FadeIn start and FadeOut end.
        /// If OnFadeOut is not completed, because presenter was enabling during fading out, the activation process will not be cancelled.
        /// </summary>
        /// <param name="positioningSupply"></param>
        /// <param name="observableEnabled"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        protected abstract UniTask OnActive(IPositioningSupply positioningSupply, AsyncCancellation cancellation);
        protected abstract UniTask OnFadeOut(Signal cancellationSignal);
        protected abstract UniTask OnRelease(Signal cancellationSignal);
        /// <summary>
        /// Immediately terminates the presenter. It should be immediately hidden and released. This method is called when the presenter is disposed.
        /// </summary>
        protected abstract void Terminate();

        #endregion
    }
}