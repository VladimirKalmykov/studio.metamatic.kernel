using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;

namespace Metamatic
{
    public class AsyncStateMachine<TState> : IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// Transition between three states: prev state, current state, next state.
        /// </summary>
        /// <summary>
        /// Transition between three states: prev state, current state, next state.
        /// </summary>
        public abstract class Transition : IDisposable
        {
            public enum Status
            {
                Idle,

                /// <summary>
                /// Transition is processing between prev state and current state
                /// </summary>
                FromPrevToCurrentProcessing,

                /// <summary>
                /// Transition is completed transition between prev state and current state
                /// </summary>
                FromPrevToCurrentCompleted,

                /// <summary>
                /// Transition is processing between current state and next state
                /// </summary>
                FromCurrentToNextProcessing,

                /// <summary>
                /// Transition is completed transition between current state and next state
                /// </summary>
                FromCurrentToNextCompleted,

                /// <summary>
                /// Transition is failed
                /// </summary>
                Error,

                /// <summary>
                /// Aborted transition (canceled)
                /// </summary>
                Aborted
            }

            protected AsyncStateMachine<TState> m_StateMachine;
            public Status status { get; private set; } = Status.Idle;
            public CancellationToken externalCancellationToken { get; protected set; }

            /// <summary>
            /// Cancellation token source will be canceled when the transition gets next state, thus transition handler can know that it should stop
            /// </summary>
            protected CancellationTokenSource m_CancellationTokenSource;

            /// <summary>
            /// Completion source for the transition from previous state to current state (will be completed when handler call WaitNextState() method or right after setting next state)
            /// </summary>
            protected UniTaskCompletionSource m_TransitionFromPrevStateCompletionSource;

            /// <summary>
            /// Completion source for the transition from current state to next state
            /// TODO: Consider to remove this completion source and use only m_TransitionFromPrevStateCompletionSource, because it is not in use
            /// </summary>
            protected UniTaskCompletionSource m_TransitionToNextStateCompletionSource;

            /// <summary>
            /// Previous state
            /// </summary>
            public TState prevState { get; protected set; }

            /// <summary>
            /// Current state
            /// </summary>
            public TState state { get; protected set; }

            /// <summary>
            /// Next state
            /// </summary>
            public TState nextState { get; protected set; }

            /// <summary>
            /// Determines that this transition already has next state
            /// </summary>
            public bool hasNextState { get; protected set; }

            /// <summary>
            /// Event to notify that next state is defined
            /// </summary>
            public event Action<TState> OnNextState;

            /// <summary>
            /// Event to notify that transition is aborted
            /// </summary>
            public event Action OnAborted;

            /// <summary>
            /// Determines that external cancellation token is canceled (thus SetState should throw OperationCanceledException)
            /// </summary>
            public bool isExternalCancellationTokenCancelled => externalCancellationToken.IsCancellationRequested;

            public UniTask completeTransitionFromPrevStateTask => m_TransitionFromPrevStateCompletionSource.Task;
            public UniTask completeTransitionToNextStateTask => m_TransitionToNextStateCompletionSource.Task;

            /// <summary>
            /// Determines that this is the initial state, therefore no previous state
            /// </summary>
            public bool isInitial { get; protected set; } = true;

            /// <summary>
            /// Determines that this is the final state, therefore no next state
            /// </summary>
            public bool isFinalize => status == Status.Aborted;

            /// <summary>
            /// CancellationToken for the transition (will be canceled when the transition gets next state)
            /// </summary>
            public CancellationToken cancellationToken => m_CancellationTokenSource.Token;

            public UniTask Task => UniTask.WhenAll(m_TransitionFromPrevStateCompletionSource.Task,
                m_TransitionToNextStateCompletionSource.Task);

            internal Transition(AsyncStateMachine<TState> stateMachine)
            {
                m_StateMachine = stateMachine;
                isInitial = true;
            }

            public void Reset(TState state, TState prevState = default, CancellationToken cancellationToken = default)
            {
                this.state = state;
                this.prevState = prevState;
                externalCancellationToken = cancellationToken;
                m_CancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                m_TransitionFromPrevStateCompletionSource = new UniTaskCompletionSource();
                m_TransitionToNextStateCompletionSource = new UniTaskCompletionSource();
                status = Status.Idle;
                hasNextState = false;
                nextState = default;
                OnNextState = null;
                OnAborted = null;
            }

            public void Reset(TState state, CancellationToken cancellationToken = default)
            {
                Reset(state, default, cancellationToken);
                hasNextState = false;
            }

            /// <summary>
            /// Method to wait for the next state (should be used by transition handler when it is ready to transition to the next state)
            /// </summary>
            /// <returns></returns>
            public UniTask<TState> WaitNextState()
            {
                if (hasNextState)
                {
                    SetStatus(Status.FromPrevToCurrentCompleted);
                    m_TransitionFromPrevStateCompletionSource.TrySetResult();
                    return UniTask.FromResult(nextState);
                }
#if DEBUG
                Debug.Assert(status == Status.FromPrevToCurrentProcessing,
                    "Transition is not processing, therefore it is not possible to wait for the next state and set status to Processed");
#endif

                var tsc = new UniTaskCompletionSource<TState>();
                OnNextState += state => { tsc.TrySetResult(state); };
                OnAborted += () =>
                {
                    Debug.Log("Abort wait next state");
                    tsc.TrySetCanceled();
                };
                SetStatus(Status.FromPrevToCurrentCompleted);
                m_TransitionFromPrevStateCompletionSource.TrySetResult();
                return tsc.Task;
            }

            /// <summary>
            /// Let handler mark that it is ready to transition to the next state. It creates copy of Transition object and detach it from a execution chain
            /// </summary>
            public void Detach()
            {
                m_TransitionFromPrevStateCompletionSource.TrySetResult();
                m_TransitionToNextStateCompletionSource.TrySetResult();
                // TODO: Implement
                throw new NotImplementedException();
            }

            protected void SetStatus(Status status)
            {
                this.status = status;
#if DEBUG
                m_StateMachine.Debug_SetTransitionStatus(status);
#endif
            }

            protected virtual void SetNextState(TState nextState)
            {
                SetStatus(Status.FromCurrentToNextProcessing);
                this.nextState = nextState;
                OnNextState?.Invoke(nextState);
                m_TransitionFromPrevStateCompletionSource.TrySetResult();
                m_CancellationTokenSource.Cancel();
            }

            public void Abort()
            {
                SetStatus(Status.Aborted);
                m_CancellationTokenSource?.Dispose();
                OnAborted?.Invoke();
            }

            public void Dispose()
            {
                m_CancellationTokenSource?.Dispose();
                m_TransitionFromPrevStateCompletionSource?.TrySetCanceled();
                m_TransitionToNextStateCompletionSource?.TrySetCanceled();
                GC.SuppressFinalize(this);
            }
        }

        public class InternalTransition : Transition
        {
            public void SetPrevState(TState prevState)
            {
                this.prevState = prevState;
            }

            public void SetNextState(TState nextState)
            {
                base.SetNextState(nextState);
            }

            public InternalTransition(AsyncStateMachine<TState> stateMachine) : base(stateMachine)
            {
            }

            public void SetTransitionFromPrevStateCompleted()
            {
                SetStatus(Status.FromPrevToCurrentCompleted);
                m_TransitionFromPrevStateCompletionSource.TrySetResult();
            }

            public void SetTransitionToNextStateCompleted()
            {
                SetStatus(Status.FromCurrentToNextCompleted);
                m_TransitionToNextStateCompletionSource.TrySetResult();
            }

            public void SetTransitionFromPrevStateError(Exception e)
            {
                SetStatus(Status.Error);
                m_TransitionFromPrevStateCompletionSource.TrySetException(e);
            }

            public void SetTransitionToNextStateError(Exception e)
            {
                SetStatus(Status.Error);
                m_TransitionToNextStateCompletionSource.TrySetException(e);
            }

            public new void SetStatus(Status status)
            {
                base.SetStatus(status);
            }
        }


        #region Delegates

        public delegate UniTask StateChangeHandler(Transition transition);

        public delegate void TransitionStepHandler(TState from, TState to);

        #endregion

        private readonly StateChangeHandler m_Handler;
        private InternalTransition m_CurrentTransition;
        private TState m_DefaultState;
        private bool m_KeepOnlyLatestState;

        /// <summary>
        /// Current processing state or the next state if transition to the next state is in process
        /// </summary>
        public TState state
        {
            get
            {
                if (m_CurrentTransition == null)
                {
                    return default;
                }

                if (m_CurrentTransition.hasNextState)
                {
                    return m_CurrentTransition.nextState;
                }

                return m_CurrentTransition.state;
            }
        }

        #region Events

        public event Action OnTransitionCompleted;
        public event TransitionStepHandler OnTransitionStep;

        #endregion


#if DEBUG
        public event Action<Transition.Status> debug_OnTransitionStatusChanged;

        internal void Debug_SetTransitionStatus(Transition.Status status)
        {
            debug_OnTransitionStatusChanged?.Invoke(status);
        }

        public DisposableAction SubscribeTransitionStatusChanged(Action<Transition.Status> onTransitionStatusChanged)
        {
            debug_OnTransitionStatusChanged += onTransitionStatusChanged;
            return new DisposableAction(() => debug_OnTransitionStatusChanged -= onTransitionStatusChanged);
        }
#endif
        /// <summary>
        /// 
        /// </summary>
        /// <param name="handler"></param>
        /// <param name="defaultState">Default state that will be used as previous state on the first set state</param>
        /// <param name="keepOnlyLatestState">Deferred queue can contain only one state (latest state win)</param>
        public AsyncStateMachine(StateChangeHandler handler, TState defaultState = default,
            bool keepOnlyLatestState = false)
        {
            this.m_Handler = handler;
            this.m_DefaultState = defaultState;
            this.m_KeepOnlyLatestState = keepOnlyLatestState;
        }

        ~AsyncStateMachine()
        {
            Dispose(true);
        }

        private Queue<(TState state, CancellationToken cancellationToken, UniTaskCompletionSource tcs)> m_StatesQueue =
            new Queue<(TState state, CancellationToken cancellationToken, UniTaskCompletionSource tcs)>();

        public UniTask SetState(TState state, CancellationToken cancellationToken = default)
        {
            if (m_CurrentTransition != null)
            {
                switch (m_CurrentTransition.status)
                {
                    case Transition.Status.FromPrevToCurrentProcessing:
                    case Transition.Status.FromPrevToCurrentCompleted:
                    {
                        /* Transition is being processed, therefore we should add next state and defer this state */
                        /* Bet first we should check if queue already has next state */
                        var tcs = new UniTaskCompletionSource();
                        var isFirstInQueue = m_StatesQueue.Count == 0;
                        EnqueueState(state, cancellationToken, tcs);
                        if (isFirstInQueue)
                        {
                            m_CurrentTransition.SetNextState(state);
                        }

                        return tcs.Task;
                    }
                    case Transition.Status.FromCurrentToNextProcessing:
                    {
                        /* Transition is in state of transitioning between current state and next state. We should add this state to queue */
                        var tcs = new UniTaskCompletionSource();
                        EnqueueState(state, cancellationToken, tcs);
                        return tcs.Task;
                    }
                }
                
                m_CurrentTransition.Reset(state, m_CurrentTransition.state, cancellationToken);
            }
            else
            {
                m_CurrentTransition = new InternalTransition(this);
                m_CurrentTransition.Reset(state, cancellationToken);
            }

            /* Create new transition and perform */
            PerformTransition().Forget();
            /* Peek next state in queue to prepare next state */
            if (m_StatesQueue.Count > 0)
            {
                var (nextState, nextCancellationToken, tcs) = m_StatesQueue.Peek();
                m_CurrentTransition.SetNextState(nextState);
            }

            return m_CurrentTransition.completeTransitionFromPrevStateTask;
        }

        private void EnqueueState(TState state, CancellationToken cancellationToken, UniTaskCompletionSource tcs)
        {
            if (m_KeepOnlyLatestState && m_StatesQueue.Count > 1)
            {
                /* Remove all states except the last one */
                while (m_StatesQueue.Count > 1)
                {
                    m_StatesQueue.Dequeue().tcs.TrySetCanceled();
                }
            }

            m_StatesQueue.Enqueue((state, cancellationToken, tcs));
        }

        private async UniTaskVoid PerformTransition()
        {
            var transition = m_CurrentTransition;
            try
            {
                transition.SetStatus(Transition.Status.FromPrevToCurrentProcessing);
                OnTransitionStep?.Invoke(transition.prevState, transition.state);
                await m_Handler(transition);
            }
            catch (OperationCanceledException)
            {
                Debug.Log("Async state machine operation canceled");
                // Skip
                if (transition.isExternalCancellationTokenCancelled &&
                    transition.status == Transition.Status.FromPrevToCurrentProcessing)
                {
                    /* Transition was cancelled by external token. Throw this token */
                    transition.SetTransitionFromPrevStateError(
                        new OperationCanceledException(transition.externalCancellationToken));
                }
                else
                {
                    /* Otherwise, it was cancelled by the transition itself, thus it should be considered as normal behaviour. Resolve it. */
                    if (transition.status == Transition.Status.FromPrevToCurrentProcessing || transition.status == Transition.Status.Aborted)
                    {
                        transition.SetTransitionFromPrevStateCompleted();
                    }
                }
            }
            catch (Exception e)
            {
                if (transition.status == Transition.Status.FromPrevToCurrentProcessing)
                {
                    transition.SetTransitionFromPrevStateError(e);
                }
                else
                {
                    transition.SetTransitionToNextStateError(e);
                }
            }
            finally
            {
                OnTransitionCompleted?.Invoke();
                transition.SetTransitionToNextStateCompleted();
                DeferCallNextStateOnCurrentTransitionReady();
            }
        }

        /// <summary>
        /// When new transition is performing, we should wait for it's readiness to apply next state (if it is in queue)
        /// </summary>
        private async void DeferCallNextStateOnCurrentTransitionReady()
        {
            try
            {
                await m_CurrentTransition.completeTransitionToNextStateTask;
                HandleNextState(true);
            }
            catch (OperationCanceledException)
            {
                HandleNextState(true);
            }
            catch (Exception e)
            {
                HandleNextState(false, e);
            }
        }

        private async void HandleNextState(bool previousIsSuccess, Exception exception = null)
        {
            if (m_StatesQueue.Count == 0)
            {
                return;
            }

            var (state, cancellationToken, tcs) = m_StatesQueue.Dequeue();
            var transitionToThisState = m_CurrentTransition.completeTransitionToNextStateTask;
            if (!previousIsSuccess)
            {
                tcs.TrySetException(exception);

                /* Throw every next state in queue */
                while (m_StatesQueue.Count > 0)
                {
                    m_StatesQueue.Dequeue().tcs.TrySetException(exception); // TODO: Cover this moment with unit tests
                }

                return;
            }

            try
            {
                await SetState(state, cancellationToken);
                tcs.TrySetResult();
            }
            catch (Exception e)
            {
                tcs.TrySetException(e);
            }
        }

        public DisposableAction SubscribeTransitionCompleted(Action onStateCompleted)
        {
            OnTransitionCompleted += onStateCompleted;
            return new DisposableAction(() => OnTransitionCompleted -= onStateCompleted);
        }

        public void Dispose(bool isFromFinalizer, bool isAsync = false)
        {
            if (isFromFinalizer)
            {
                throw new InvalidOperationException("AsyncStateMachine should be disposed manually");
            }

            if (!isAsync)
            {
                m_CurrentTransition?.Dispose();
            }

            GC.SuppressFinalize(this);
        }

        public void Dispose()
        {
            Dispose(false);
        }

        public async ValueTask DisposeAsync()
        {
            Dispose(false, true);
            if (m_CurrentTransition != null)
            {
                m_CurrentTransition.Abort();
                try
                {
                    await m_CurrentTransition.Task;
                }
                catch (OperationCanceledException)
                {
                    // Ignore
                }
                // TODO: Consider do not throw exception of current transition
            }
        }
    }
}