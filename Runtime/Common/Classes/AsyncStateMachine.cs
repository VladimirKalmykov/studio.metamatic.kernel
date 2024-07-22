using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic
{
    public sealed class AsyncStateMachine<TState> : IDisposable, IAsyncDisposable
    {
        public enum TransitionStatus
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

        public readonly struct Transition
        {
            private readonly AsyncStateMachine<TState> m_StateMachine;

            public TransitionStatus status => m_StateMachine.m_Status;
            public CancellationToken externalCancellationToken => m_StateMachine.m_ExternalCancellationToken;
            public TState prevState => m_StateMachine.m_PrevState;
            public TState state => m_StateMachine.m_State;
            public TState nextState => m_StateMachine.m_NextState;
            public bool hasNextState => m_StateMachine.m_HasNextState;

            public bool isExternalCancellationTokenCancelled =>
                m_StateMachine.m_ExternalCancellationToken.IsCancellationRequested;

            public UniTask completeTransitionFromPrevStateTask => m_StateMachine.completeTransitionFromPrevStateTask;
            public UniTask completeTransitionToNextStateTask => m_StateMachine.completeTransitionToNextStateTask;
            public bool isInitial => m_StateMachine.isInitial;
            public bool isFinalize => m_StateMachine.m_Status == TransitionStatus.Aborted;
            public CancellationToken cancellationToken => m_StateMachine.cancellationToken;
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public UniTask<TState> WaitNextState() => m_StateMachine.WaitNextState();

            internal Transition(AsyncStateMachine<TState> stateMachine)
            {
                m_StateMachine = stateMachine;
            }
        }

        private TransitionStatus m_Status = TransitionStatus.Idle;
        private CancellationToken m_ExternalCancellationToken;

        /// <summary>
        /// Cancellation token source will be canceled when the transition gets next state, thus transition handler can know that it should stop
        /// </summary>
        private CancellationTokenSource m_CancellationTokenSource;

        /// <summary>
        /// Completion source for the transition from previous state to current state (will be completed when handler call WaitNextState() method or right after setting next state)
        /// </summary>
        private UniTaskCompletionSource m_TransitionFromPrevStateCompletionSource;

        /// <summary>
        /// Completion source for the transition from current state to next state
        /// TODO: Consider to remove this completion source and use only m_TransitionFromPrevStateCompletionSource, because it is not in use
        /// </summary>
        private UniTaskCompletionSource m_TransitionToNextStateCompletionSource;

        /// <summary>
        /// Previous state
        /// </summary>
        private TState m_PrevState;

        /// <summary>
        /// Current state
        /// </summary>
        private TState m_State;

        /// <summary>
        /// Next state
        /// </summary>
        private TState m_NextState;

        private bool m_HasPrevState;

        /// <summary>
        /// Determines that this transition already has next state
        /// </summary>
        private bool m_HasNextState;

        /// <summary>
        /// Event to notify that next state is defined
        /// </summary>
        private event Action<TState> OnNextState;

        /// <summary>
        /// Event to notify that transition is aborted
        /// </summary>
        private event Action OnAborted;

        /// <summary>
        /// Determines that external cancellation token is canceled (thus SetState should throw OperationCanceledException)
        /// </summary>
        private bool isExternalCancellationTokenCancelled => m_ExternalCancellationToken.IsCancellationRequested;

        private UniTask completeTransitionFromPrevStateTask => m_TransitionFromPrevStateCompletionSource.Task;
        private UniTask completeTransitionToNextStateTask => m_TransitionToNextStateCompletionSource.Task;

        /// <summary>
        /// Determines that this is the initial state, therefore no previous state
        /// </summary>
        private bool isInitial => !m_HasPrevState;

        /// <summary>
        /// Determines that this is the final state, therefore no next state
        /// </summary>
        public bool isFinalize => m_Status == TransitionStatus.Aborted;

        /// <summary>
        /// CancellationToken for the transition (will be canceled when the transition gets next state)
        /// </summary>
        private CancellationToken cancellationToken => m_CancellationTokenSource.Token;

        private UniTask task => UniTask.WhenAll(m_TransitionFromPrevStateCompletionSource.Task,
            m_TransitionToNextStateCompletionSource.Task);

        private void Reset(TState nextState, TState nextPrevState = default, CancellationToken externalCancellationToken = default)
        {
            this.m_State = nextState;
            this.m_PrevState = nextPrevState;
            m_ExternalCancellationToken = externalCancellationToken;
            m_CancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(externalCancellationToken);
            m_TransitionFromPrevStateCompletionSource = new UniTaskCompletionSource();
            m_TransitionToNextStateCompletionSource = new UniTaskCompletionSource();
            m_Status = TransitionStatus.Idle;
            m_HasNextState = false;
            m_HasPrevState = true;
            m_NextState = default;
            OnNextState = null;
            OnAborted = null;
        }

        private void Reset(TState nextState, CancellationToken externalCancellationToken = default)
        {
            Reset(nextState, default, externalCancellationToken);
            m_PrevState = m_DefaultState;
            m_HasPrevState = false;
        }

        /// <summary>
        /// Method to wait for the next state (should be used by transition handler when it is ready to transition to the next state)
        /// </summary>
        /// <returns></returns>
        public UniTask<TState> WaitNextState()
        {
            if (m_HasNextState)
            {
                SetStatus(TransitionStatus.FromPrevToCurrentCompleted);
                m_TransitionFromPrevStateCompletionSource.TrySetResult();
                return UniTask.FromResult(m_NextState);
            }
#if DEBUG
            Debug.Assert(m_Status == TransitionStatus.FromPrevToCurrentProcessing,
                "Transition is not processing, therefore it is not possible to wait for the next state and set status to Processed");
#endif

            var tsc = new UniTaskCompletionSource<TState>();
            OnNextState += newNextState => { tsc.TrySetResult(newNextState); };
            OnAborted += () =>
            {
                Debug.Log("Abort wait next state");
                tsc.TrySetCanceled();
            };
            SetStatus(TransitionStatus.FromPrevToCurrentCompleted);
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

        private void SetStatus(TransitionStatus status)
        {
            this.m_Status = status;
#if DEBUG
            Debug_SetTransitionStatus(status);
#endif
        }

        private void SetNextState(TState nextState)
        {
            SetStatus(TransitionStatus.FromCurrentToNextProcessing);
            this.m_NextState = nextState;
            OnNextState?.Invoke(nextState);
            m_TransitionFromPrevStateCompletionSource.TrySetResult();
            m_CancellationTokenSource.Cancel();
        }

        private void Abort()
        {
            SetStatus(TransitionStatus.Aborted);
            m_CancellationTokenSource?.Dispose();
            OnAborted?.Invoke();
        }

        public void SetPrevState(TState prevState)
        {
            this.m_PrevState = prevState;
        }

        private void SetTransitionFromPrevStateCompleted()
        {
            SetStatus(TransitionStatus.FromPrevToCurrentCompleted);
            m_TransitionFromPrevStateCompletionSource.TrySetResult();
        }

        private void SetTransitionToNextStateCompleted()
        {
            SetStatus(TransitionStatus.FromCurrentToNextCompleted);
            m_TransitionToNextStateCompletionSource.TrySetResult();
        }

        private void SetTransitionFromPrevStateError(Exception e)
        {
            SetStatus(TransitionStatus.Error);
            m_TransitionFromPrevStateCompletionSource.TrySetException(e);
        }

        private void SetTransitionToNextStateError(Exception e)
        {
            SetStatus(TransitionStatus.Error);
            m_TransitionToNextStateCompletionSource.TrySetException(e);
        }


        #region Delegates

        public delegate UniTask StateChangeHandler(Transition transition);

        public delegate void TransitionStepHandler(TState from, TState to);

        #endregion

        private readonly StateChangeHandler m_Handler;
        private TState m_DefaultState;
        private bool m_KeepOnlyLatestState;

        /// <summary>
        /// Current processing state or the next state if transition to the next state is in process
        /// </summary>
        public TState state
        {
            get
            {
                if (m_HasNextState)
                {
                    return m_NextState;
                }

                return m_State;
            }
        }

        #region Events

        public event Action OnTransitionCompleted;
        public event TransitionStepHandler OnTransitionStep;

        #endregion


#if DEBUG
        public event Action<TransitionStatus> DebugOnTransitionStatusChanged;

        private void Debug_SetTransitionStatus(TransitionStatus status)
        {
            DebugOnTransitionStatusChanged?.Invoke(status);
        }

        public DisposableAction SubscribeTransitionStatusChanged(Action<TransitionStatus> onTransitionStatusChanged)
        {
            DebugOnTransitionStatusChanged += onTransitionStatusChanged;
            return new DisposableAction(() => DebugOnTransitionStatusChanged -= onTransitionStatusChanged);
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
            throw new InvalidOperationException("AsyncStateMachine should be disposed manually");
        }

        private Queue<(TState state, CancellationToken cancellationToken, UniTaskCompletionSource tcs)> m_StatesQueue =
            new Queue<(TState state, CancellationToken cancellationToken, UniTaskCompletionSource tcs)>();

        public UniTask SetState(TState state, CancellationToken externalCancellationToken = default)
        {
            if (m_Status != TransitionStatus.Idle)
            {
                switch (m_Status)
                {
                    case TransitionStatus.FromPrevToCurrentProcessing:
                    case TransitionStatus.FromPrevToCurrentCompleted:
                    {
                        /* Transition is being processed, therefore we should add next state and defer this state */
                        /* Bet first we should check if queue already has next state */
                        var tcs = new UniTaskCompletionSource();
                        var isFirstInQueue = m_StatesQueue.Count == 0;
                        EnqueueState(state, externalCancellationToken, tcs);
                        if (isFirstInQueue)
                        {
                            SetNextState(state);
                        }

                        return tcs.Task;
                    }
                    case TransitionStatus.FromCurrentToNextProcessing:
                    {
                        /* Transition is in state of transitioning between current state and next state. We should add this state to queue */
                        var tcs = new UniTaskCompletionSource();
                        EnqueueState(state, externalCancellationToken, tcs);
                        return tcs.Task;
                    }
                }

                Reset(state, m_State, externalCancellationToken);
            }
            else
            {
                Reset(state, externalCancellationToken);
            }

            /* Create new transition and perform */
            PerformTransition().Forget();
            /* Peek next state in queue to prepare next state */
            if (m_StatesQueue.Count > 0)
            {
                var (nextState, _, _) = m_StatesQueue.Peek();
                SetNextState(nextState);
            }

            return completeTransitionFromPrevStateTask;
        }

        private void EnqueueState(TState state, CancellationToken externalCancellationToken, UniTaskCompletionSource tcs)
        {
            if (m_KeepOnlyLatestState && m_StatesQueue.Count > 1)
            {
                /* Remove all states except the last one */
                while (m_StatesQueue.Count > 1)
                {
                    m_StatesQueue.Dequeue().tcs.TrySetCanceled();
                }
            }

            m_StatesQueue.Enqueue((state, externalCancellationToken, tcs));
        }

        private async UniTaskVoid PerformTransition()
        {
            try
            {
                SetStatus(TransitionStatus.FromPrevToCurrentProcessing);
                OnTransitionStep?.Invoke(m_PrevState, m_State);
                await m_Handler(new Transition(this));
            }
            catch (OperationCanceledException)
            {
                Debug.Log("Async state machine operation canceled");
                // Skip
                if (isExternalCancellationTokenCancelled &&
                    m_Status == TransitionStatus.FromPrevToCurrentProcessing)
                {
                    /* Transition was cancelled by external token. Throw this token */
                    SetTransitionFromPrevStateError(
                        new OperationCanceledException(m_ExternalCancellationToken));
                }
                else
                {
                    /* Otherwise, it was cancelled by the transition itself, thus it should be considered as normal behaviour. Resolve it. */
                    if (m_Status == TransitionStatus.FromPrevToCurrentProcessing || m_Status == TransitionStatus.Aborted)
                    {
                        SetTransitionFromPrevStateCompleted();
                    }
                }
            }
            catch (Exception e)
            {
                if (m_Status == TransitionStatus.FromPrevToCurrentProcessing)
                {
                    SetTransitionFromPrevStateError(e);
                }
                else
                {
                    SetTransitionToNextStateError(e);
                }
            }
            finally
            {
                OnTransitionCompleted?.Invoke();
                SetTransitionToNextStateCompleted();
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
                await completeTransitionToNextStateTask;
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

            var (state, externalCancellationToken, tcs) = m_StatesQueue.Dequeue();
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
                await SetState(state, externalCancellationToken);
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

        private void Dispose(bool isAsync)
        {
            if (!isAsync)
            {
                m_CancellationTokenSource?.Dispose();
                m_TransitionFromPrevStateCompletionSource?.TrySetCanceled();
                m_TransitionToNextStateCompletionSource?.TrySetCanceled();
            }

            GC.SuppressFinalize(this);
        }

        public void Dispose()
        {
            Dispose(false);
        }

        public async ValueTask DisposeAsync()
        {
            Dispose(true);
            Abort();
            try
            {
                await task;
            }
            catch (OperationCanceledException)
            {
                // Ignore
            }
            // TODO: Consider do not throw exception of current transition
        }
    }
}