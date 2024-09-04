using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Cysharp.Threading.Tasks;
using Metamatic.DevTools;
using Metamatic.Exceptions;
using UnityEngine;

namespace Metamatic
{
    public sealed class AsyncStateMachine<TState> : IDisposable, IUniTaskAsyncDisposable
    {
        public enum TransitionStatus
        {
            Initial,

            /// <summary>
            /// When there is no transitions is progress, but already has previous state
            /// </summary>
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
            public readonly ulong ID;

            public TransitionStatus status => m_StateMachine.m_Status;
            public CancellationToken externalCancellationToken => m_StateMachine.m_ExternalCancellationToken;
            public TState prevState => m_StateMachine.prevState;
            public TState state => m_StateMachine.currentState;
            public TState nextState => m_StateMachine.nextState;
            public bool hasNextState => m_StateMachine.m_HasNextState;

            public bool isExternalCancellationTokenCancelled =>
                m_StateMachine.m_ExternalCancellationToken.IsCancellationRequested;

            public UniTask completeTransitionFromPrevStateTask => m_StateMachine.completeTransitionFromPrevStateTask;
            public UniTask completeTransitionToNextStateTask => m_StateMachine.completeTransitionToNextStateTask;
            public bool isInitial => m_StateMachine.isInitial;
            public bool isFinalize => m_StateMachine.m_Status == TransitionStatus.Aborted;
            public CancellationToken cancellationToken => m_StateMachine.cancellationToken;
            public CancellationToken generalCancellationToken => m_StateMachine.internalCancellationToken;
            public bool isAborted => m_StateMachine.m_Status == TransitionStatus.Aborted;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public UniTask<TState> WaitNextState() => m_StateMachine.WaitNextState();

            public DisposableAction AddNextStateValidator(Validator<TState, TState>.ValidatorDelegate validator)
            {
                return m_StateMachine.AddNextStateValidator(validator);
            }

            internal Transition(AsyncStateMachine<TState> stateMachine, ulong id)
            {
                m_StateMachine = stateMachine;
                ID = id;
            }
        }

        private readonly bool m_StateIsDisposable;

        private TransitionStatus m_Status = TransitionStatus.Initial;

        /// <summary>
        /// Internal cancellation token for the whole state machine
        /// </summary>
        private CancellationTokenSource m_InternalCancellationTokenSource = new();

        /// <summary>
        /// External cancellation token passed within the state
        /// </summary>
        private CancellationToken m_ExternalCancellationToken;

        /// <summary>
        /// Cancellation token source will be canceled when the transition gets next state, thus transition handler can know that it should stop
        /// </summary>
        private CancellationTokenSource m_CancellationTokenSource;

        /// <summary>
        /// Completion source for the transition from previous state to current state (will be completed when handler call WaitNextState() method or right after setting next state)
        /// </summary>
        private ReusableUniTaskCompletionSource m_TransitionFromPrevStateCompletionSource;

        /// <summary>
        /// Completion source for the transition from current state to next state
        /// TODO: Consider to remove this completion source and use only m_TransitionFromPrevStateCompletionSource, because it is not in use
        /// </summary>
        private ReusableUniTaskCompletionSource m_TransitionToNextStateCompletionSource;

        /// <summary>
        /// Indicated that transition from previous state to current state is resolved (to avoid using return value of TrySetResult)
        /// </summary>
        private bool m_TransitionFromPrevStateResolved = false;

        /// <summary>
        /// Indicated that transition from current state to next state is resolved (to avoid using return value of TrySetResult)
        /// </summary>
        private bool m_TransitionToNextStateResolved = false;

        private StateValidationHandler m_CommonNextStateValidationHandler;

        private Validator<TState, TState> m_NextStateValidator = new();

        /// <summary>
        /// Previous state
        /// </summary>
        public TState prevState { get; private set; }

        /// <summary>
        /// Current state
        /// </summary>
        public TState currentState { get; private set; }

        /// <summary>
        /// Next state
        /// </summary>
        public TState nextState { get; private set; }

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
        /// Last generated id
        /// </summary>
        private ulong m_LastId;

        /// <summary>
        /// State ID that is currently active
        /// </summary>
        private ulong activeId { get; set; }

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
        
        private bool m_Disposed;

        /// <summary>
        /// Returns current state or next state if it is defined
        /// </summary>
        public TState state
        {
            get
            {
                if (m_HasNextState) return nextState;
                return currentState;
            }
        }

        /// <summary>
        /// CancellationToken for the transition (will be canceled when the transition gets next state)
        /// </summary>
        private CancellationToken cancellationToken => m_CancellationTokenSource.Token;

        /// <summary>
        /// Internal cancellation token for the whole state machine (will be canceled when state machine is disposed)
        /// </summary>
        public CancellationToken internalCancellationToken => m_InternalCancellationTokenSource.Token;

        private UniTask task =>
            UniTask.WhenAll(m_TransitionFromPrevStateCompletionSource?.Task ?? UniTask.CompletedTask,
                m_TransitionToNextStateCompletionSource?.Task ?? UniTask.CompletedTask);

        public event Action<TState> OnStateChange;

        private void Reset(TState nextState, TState nextPrevState = default,
            CancellationToken externalCancellationToken = default)
        {
            if (m_SettingDebug)
                Debug.Log($"[ASYNC-STATE-MACHINE] Reset (new prevState: {nextPrevState}, nextState: {nextState}");
#if DEBUG
            try
            {
                /* Dispose prev state if it supports IDisposable */
                if (m_StateIsDisposable && m_HasPrevState)
                {
                    (prevState as IDisposable)?.Dispose();
                }
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"Error while disposing prev state {prevState} during Reset to next state {nextState}: {e.Message}");
            }
#else
            /* Dispose prev state if it supports IDisposable */
            if (m_StateIsDisposable && m_HasPrevState)
            {
                (prevState as IDisposable)?.Dispose();
            }
#endif

            this.currentState = nextState;
            OnStateChange?.Invoke(nextState);
            this.prevState = nextPrevState;
            m_ExternalCancellationToken = externalCancellationToken;
            m_CancellationTokenSource?.Dispose();
            m_CancellationTokenSource =
                CancellationTokenSource.CreateLinkedTokenSource(m_InternalCancellationTokenSource.Token,
                    externalCancellationToken);
            
            if (m_TransitionFromPrevStateCompletionSource == null)
            {
                m_TransitionFromPrevStateCompletionSource = ReusableUniTaskCompletionSource.Create();
                #if DEBUG
                if (m_SettingDebug)
                    Debug.Log($"[ASYNC-STATE-MACHINE][TASK] Init transition from prev state completion source #{m_TransitionFromPrevStateCompletionSource.debugVersion}");
                #endif
            }
            else
            {
                m_TransitionFromPrevStateCompletionSource.Reset();
#if DEBUG
                if (m_SettingDebug)
                    Debug.Log($"[ASYNC-STATE-MACHINE][TASK] Reset transition from prev state completion source #{m_TransitionFromPrevStateCompletionSource.debugVersion}");
#endif
            }
            
            if (m_TransitionToNextStateCompletionSource == null)
            {
                m_TransitionToNextStateCompletionSource = ReusableUniTaskCompletionSource.Create();
#if DEBUG
                if (m_SettingDebug)
                    Debug.Log($"[ASYNC-STATE-MACHINE][TASK] Init transition from current state to next state completion source #{m_TransitionToNextStateCompletionSource.debugVersion}");
#endif
            }
            else
            {
                m_TransitionToNextStateCompletionSource.Reset();
#if DEBUG
                if (m_SettingDebug)
                    Debug.Log($"[ASYNC-STATE-MACHINE][TASK] Reset transition from current state to next state completion source #{m_TransitionToNextStateCompletionSource.debugVersion}");
#endif
            }
            m_TransitionFromPrevStateResolved = false;
            m_TransitionToNextStateResolved = false;
            m_Status = TransitionStatus.Idle;
            m_HasNextState = false;
            m_HasPrevState = true;
            this.nextState = default;
            OnNextState = null;
            OnAborted = null;
        }

        private void Reset(TState nextState, CancellationToken externalCancellationToken = default)
        {
            Reset(nextState, default, externalCancellationToken);
            prevState = m_DefaultState;
            m_HasPrevState = false;
        }

        /// <summary>
        /// Add validation function that validates every next state. If it returns false, the state will be ignored. You can add multiple validators, all of them should return true to pass the validation. Don't forget to dispose the returned object to remove the validator.
        /// </summary>
        /// <param name="validator"></param>
        /// <returns></returns>
        public DisposableAction AddNextStateValidator(Validator<TState, TState>.ValidatorDelegate validator
#if DEBUG
            ,
            string debugId = ""
#endif
        )
        {
#if DEBUG
            var dispose = m_NextStateValidator.Add(validator);
            return new DisposableAction(() => { dispose.Dispose(); });
#else
            return m_NextStateValidator.Add(validator);
#endif
        }

        /// <summary>
        /// Let handler mark that it is ready to transition to the next state. It creates copy of Transition object and detach it from a execution chain
        /// </summary>
        public void Detach()
        {
            m_TransitionFromPrevStateResolved = true;
            m_TransitionFromPrevStateCompletionSource.TrySetResult();
            m_TransitionToNextStateResolved = true;
            m_TransitionToNextStateCompletionSource.TrySetResult();
            // TODO: Implement
            throw new NotImplementedException();
        }

        private void SetStatus(
            TransitionStatus status
#if DEBUG
            , string source
#endif
        )
        {
            this.m_Status = status;
#if DEBUG
            if (m_SettingDebug)
                Debug.Log(
                    $"[ASYNC-STATE-MACHINE] Set status {status} (prev: {prevState}, current: {currentState}, next: {nextState}) Source: {source}");
            Debug_SetTransitionStatus(status, source);
#endif
        }

        /// <summary>
        /// Method to wait for the next state (should be used by transition handler when it is ready to transition to the next state)
        /// </summary>
        /// <returns></returns>
        public UniTask<TState> WaitNextState()
        {
            if (m_HasNextState)
            {
                TrySetTransitionFromPrevStateCompleted(0);
#if DEBUG
                SetStatus(TransitionStatus.FromCurrentToNextProcessing, "WaitNextState");
#else
                SetStatus(TransitionStatus.FromCurrentToNextProcessing);
#endif
                return UniTask.FromResult(nextState);
            }
#if DEBUG
            string statusHistoryText = string.Join(" -> ",
                System.Linq.Enumerable.Select(m_TransitionStatusQueue.originalQueue, x => $"{x.Item1} ({x.Item2})"));
            if (m_Status == TransitionStatus.Aborted)
            {
                var abortedTask = AutoResetUniTaskCompletionSource<TState>.Create();
                abortedTask.TrySetException(new OperationAborted("Async state machine is aborted"));
                return abortedTask.Task;
            }
            else
            {
                Debug.Assert(m_Status == TransitionStatus.FromPrevToCurrentProcessing,
                    $"Transition is not processing, therefore it is not possible to wait for the next state and set status to Processed. Got status: {m_Status}, prevState: {prevState}, currentState: {currentState}, nextState: {nextState} ({(m_HasNextState ? "defined" : "default")}) Status history: {statusHistoryText}");
            }
#endif

            var tsc = AutoResetUniTaskCompletionSource<TState>.Create();
            OnNextState += newNextState =>
            {
                /* If we are here, it means that the transition from previous state is already completed. But we still cancel token, because it should be cancelled before resolving next state waiter task */
                m_CancellationTokenSource.Cancel();
                tsc.TrySetResult(newNextState);
            };
            OnAborted += () =>
            {
                Debug.Log("Abort wait next state");
                tsc.TrySetCanceled();
            };

            TrySetTransitionFromPrevStateCompleted(0);

            return tsc.Task;
        }

        private bool SetNextState(TState nextState, bool skipValidationCheck = false)
        {
            #if DEBUG
            if (m_HasNextState) Debug.LogError("Already has next state");
            if (m_SettingDebug) Debug.Log($"[ASYNC-STATE-MACHINE] Set next state: {nextState}");
            #endif
            /* Invalidate state */
            if (!skipValidationCheck && !m_NextStateValidator.Validate(state, nextState))
            {
                /* As the state is invalid, we should dispose it if it supports IDisposable */
                if (m_StateIsDisposable)
                {
                    (nextState as IDisposable)?.Dispose();
                }

                return false;
            }

            this.nextState = nextState;
            this.m_HasNextState = true;

            if (m_Status == TransitionStatus.FromPrevToCurrentCompleted)
            {
#if DEBUG
                SetStatus(TransitionStatus.FromCurrentToNextProcessing, "SetNextState");
#else
                SetStatus(TransitionStatus.FromCurrentToNextProcessing);
#endif
            }

            var memoizedCancellationTokenSource = m_CancellationTokenSource;
            OnNextState?.Invoke(nextState);
#if DEBUG
            if (m_SettingDebug)
            {
                Debug.Log($"[ASYNC-STATE-MACHINE] CANCEL because next state is defined: {nextState}");
            }
#endif
            if (!memoizedCancellationTokenSource.IsCancellationRequested)
            {
                memoizedCancellationTokenSource.Cancel();
            }
            
            return true;
        }

        private void Abort()
        {
#if DEBUG
            SetStatus(TransitionStatus.Aborted, "Abort");
#else
            SetStatus(TransitionStatus.Aborted);
#endif

            m_CancellationTokenSource?.Dispose();
            OnAborted?.Invoke();
        }

        public void SetPrevState(TState prevState)
        {
            this.prevState = prevState;
        }

        private void TrySetTransitionFromPrevStateCompleted(ulong transitionId)
        {
            if (!m_TransitionFromPrevStateResolved)
            {
                if (m_SettingDebug)
                    Debug.Log($"[ASYNC-STATE-MACHINE] Complete transition #{transitionId} from prev to current");
#if DEBUG
                SetStatus(TransitionStatus.FromPrevToCurrentCompleted, "SetTransitionFromPrevStateCompleted");
#else
            SetStatus(TransitionStatus.FromPrevToCurrentCompleted);
#endif
                m_TransitionFromPrevStateResolved = true;
                m_TransitionFromPrevStateCompletionSource.TrySetResult();
            }
        }

        private void SetTransitionToNextStateCompleted(ulong transitionId)
        {
            if (!m_TransitionToNextStateResolved)
            {
                if (m_SettingDebug)
                    Debug.Log($"[ASYNC-STATE-MACHINE] Complete transition #{transitionId} from current to next");
#if DEBUG
                SetStatus(TransitionStatus.FromCurrentToNextCompleted, "SetTransitionToNextStateCompleted");
#else
            SetStatus(TransitionStatus.FromCurrentToNextCompleted);
#endif
                m_TransitionToNextStateResolved = true;
                m_TransitionToNextStateCompletionSource.TrySetResult();
            }
        }

        private void SetTransitionFromPrevStateError(Exception e)
        {
#if DEBUG
            SetStatus(TransitionStatus.Error, $"SetTransitionFromPrevStateError: {e.Message}");
#else
            SetStatus(TransitionStatus.Error);
#endif
            if (m_TransitionFromPrevStateResolved)
            {
                throw new Exception(
                    "Transition from previous state to current throws exception, but its completion source is already resolved");
            }

            m_TransitionFromPrevStateResolved = true;
            m_TransitionFromPrevStateCompletionSource.TrySetException(e);
        }

        private void SetTransitionToNextStateError(Exception e)
        {
#if DEBUG
            SetStatus(TransitionStatus.Error, $"SetTransitionToNextStateError: {e.Message}");
#else
            SetStatus(TransitionStatus.Error);
#endif
            m_TransitionToNextStateCompletionSource.TrySetException(e);
        }


        #region Delegates

        public delegate UniTask StateChangeHandler(Transition transition);

        public delegate void TransitionHandler(TState from, TState to);

        public delegate bool StateValidationHandler(TState state);

        #endregion

        private readonly StateChangeHandler m_Handler;
        private TState m_DefaultState;
        private bool m_SettingKeepOnlyLatestState;
        private bool m_SettingAutoCompleteWithDefaultState;
        private bool m_SettingDebug;

        /// <summary>
        /// Determines that state machine is autocompleted with default state (according to m_SettingAutoCompleteWithDefaultState). This property is needed to avoid adding default state to the queue when it is already added that can cause infinite loop
        /// </summary>
        private ulong m_LatestTransitionId = 0;

        private ulong m_AutocompleteTransitionId = 0;

        #region Events

        public event Action OnTransitionCompleted;
        public event TransitionHandler OnTransitionStep;

        #endregion


#if DEBUG
        public event Action<TransitionStatus> DebugOnTransitionStatusChanged;

        private LimitedQueue<(TransitionStatus, string)> m_TransitionStatusQueue =
            new LimitedQueue<(TransitionStatus, string)>(5);

        private void Debug_SetTransitionStatus(TransitionStatus status, string source)
        {
            m_TransitionStatusQueue.Enqueue((status, source));
            DebugOnTransitionStatusChanged?.Invoke(status);
        }

        public DisposableAction Debug_SubscribeTransitionStatusChanged(
            Action<TransitionStatus> onTransitionStatusChanged)
        {
            DebugOnTransitionStatusChanged += onTransitionStatusChanged;
            return new DisposableAction(() => DebugOnTransitionStatusChanged -= onTransitionStatusChanged);
        }

        private DisposableAction m_Debug_UILoggerDisposable;

#endif
        /// <summary>
        /// 
        /// </summary>
        /// <param name="handler"></param>
        /// <param name="defaultState">Default state that will be used as previous state on the first set state</param>
        /// <param name="keepOnlyLatestState">Deferred queue can contain only one state (latest state win). Otherwise the queue of the next state has no limits</param>
        /// <param name="autoCompleteWithDefaultState">If current transition is about to end, but there are no next states in queue, automatically add default state as next state</param>
        public AsyncStateMachine(StateChangeHandler handler, TState defaultState = default,
            bool keepOnlyLatestState = false, bool autoCompleteWithDefaultState = false, bool debug = false)
        {
            this.m_Handler = handler;
            this.m_DefaultState = defaultState;
            this.m_SettingKeepOnlyLatestState = keepOnlyLatestState;
            this.m_SettingAutoCompleteWithDefaultState = autoCompleteWithDefaultState;
            this.m_SettingDebug = debug;
            this.currentState = defaultState;
            OnStateChange?.Invoke(defaultState);
            this.m_StateIsDisposable = typeof(IDisposable).IsAssignableFrom(typeof(TState));

#if DEBUG
            if (m_SettingDebug)
            {
                m_NextStateValidator.debug = true;
                m_Debug_UILoggerDisposable = Monitoring.Create("Async state machine", "Validator",
                    () => m_NextStateValidator.ToString());
            }
#endif
        }

        ~AsyncStateMachine()
        {
            throw new InvalidOperationException("AsyncStateMachine should be disposed manually");
        }

        private ulong GenerateId()
        {
            return ++m_LastId == ulong.MaxValue ? m_LastId = 1 : m_LastId;
        }

        private Queue<(ulong id, TState state, CancellationToken cancellationToken, ReusableUniTaskCompletionSource tcs)>
            m_StatesQueue =
                new();

        /// <summary>
        /// Add new state to the queue
        /// </summary>
        /// <param name="state"></param>
        /// <param name="externalCancellationToken"></param>
        /// <param name="id"></param>
        /// <returns>Uni task for transition between prev task to this task</returns>
        public UniTask PushLegacy(TState state, CancellationToken externalCancellationToken = default, ulong id = 0)
        {
            return PushStateAsync(state, externalCancellationToken, id).task;
        }

        /// <summary>
        /// Add next state to the queue if current state matches the specified state
        /// </summary>
        /// <param name="ifCurrentStateIs"></param>
        /// <param name="nextState"></param>
        /// <param name="externalCancellationToken"></param>
        /// <returns></returns>
        public UniTask TryPushNextState(TState ifCurrentStateIs, TState nextState,
            CancellationToken externalCancellationToken = default)
        {
            if (this.currentState.Equals(ifCurrentStateIs) && m_StatesQueue.Count == 0)
            {
                return PushLegacy(nextState, externalCancellationToken);
            }

            return UniTask.CompletedTask;
        }

        /// <summary>
        /// This method allows you to push next state right after transition with specified ID
        /// E.g.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ulong TryPushNextState(ulong id, TState nextState, CancellationToken cancellationToken = default)
        {
            if (IsLatest(id))
            {
                return PushState(nextState, cancellationToken);
            }

            return 0;
        }

        /// <summary>
        /// More convenient method to add state to the queue and get its id
        /// In the future versions this method can stay as the only method to add state, because its interface is more convenient to reduce the number of created UniTaskCompletionSources per flow of the state machine
        /// </summary>
        /// <param name="state"></param>
        /// <param name="externalCancellationToken"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public ulong PushState(TState state, CancellationToken externalCancellationToken = default, ulong id = 0)
        {
            return PushStateAsync(state, externalCancellationToken, id).id;
        }

        /// <summary>
        /// Add new state to the queue and get its task and id
        /// </summary>
        /// <param name="state"></param>
        /// <param name="externalCancellationToken"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public (UniTask task, ulong id) PushStateAsync(TState state,
            CancellationToken externalCancellationToken = default,
            ulong id = 0)
        {
#if DEBUG
            if (m_SettingDebug)
                Debug.Log(
                    $"[ASYNC-STATE-MACHINE] Validate attempt to add state {state} (id #{id}) (Curren status is {m_Status})");
#endif
            /* Invalidate state */
            if (m_Disposed || !m_NextStateValidator.Validate(currentState, state))
            {
                /* As the state is invalid, we should dispose it if it supports IDisposable */
                if (m_StateIsDisposable)
                {
#if DEBUG
                    try
                    {
                        (state as IDisposable)?.Dispose();
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(
                            $"[ASYNC-STATE-MACHINE] Error while disposing invalid state {state}: {e.Message}");
                    }
#else
                    (state as IDisposable)?.Dispose();
#endif
                }
                var resultTask = m_Disposed
                    ? UniTask.FromException(new OperationCanceledException("State machine is disposed"))
                    : UniTask.CompletedTask;
                return (UniTask.CompletedTask, 0);
            }
#if DEBUG
            if (m_SettingDebug)
                Debug.Log($"[ASYNC-STATE-MACHINE] Add valid state {state} (id #{id}) (Curren status is {m_Status})");
#endif

            /* Generate unique Id the future transition */
            var nextId = id == 0 ? GenerateId() : id;
            if (m_Status != TransitionStatus.Initial)
            {
                switch (m_Status)
                {
                    case TransitionStatus.FromPrevToCurrentProcessing:
                    case TransitionStatus.FromPrevToCurrentCompleted:
                    {
                        /* Transition is being processed, therefore we should add next state and defer this state */
                        /* Bet first we should check if queue already has next state */
                        var tcs = ReusableUniTaskCompletionSource.Create();
                        #if DEBUG
                        if (m_SettingDebug)
                            Debug.Log($"[ASYNC-STATE-MACHINE][TASK] Init task completion source #{tcs.debugVersion} for {state}(id #{id}) (Curren status is {m_Status})");
                        #endif
                        var isFirstInQueue = m_StatesQueue.Count == 0;
                        EnqueueState(nextId, state, externalCancellationToken, tcs);
                        if (isFirstInQueue)
                        {
                            // Do not need to check validation handler, because it was checked before
                            SetNextState(state, true);
                        }

                        return (tcs.Task, nextId);
                    }
                    case TransitionStatus.FromCurrentToNextProcessing:
                    case TransitionStatus.FromCurrentToNextCompleted:
                    case TransitionStatus.Error:
                    {
                        /* Transition is in state of transitioning between current state and next state. We should add this state to queue */
                        var tcs = ReusableUniTaskCompletionSource.Create();
#if DEBUG
                        if (m_SettingDebug)
                            Debug.Log($"[ASYNC-STATE-MACHINE][TASK] Init task completion source #{tcs.debugVersion} for {state}(id #{id}) (Curren status is {m_Status})");
#endif
                        EnqueueState(nextId, state, externalCancellationToken, tcs);
                        return (tcs.Task, nextId);
                    }
                }

                Reset(state, this.currentState, externalCancellationToken);
            }
            else
            {
                Reset(state, externalCancellationToken);
            }

            /* Create new transition and perform */
            PerformTransition(nextId).Forget();
            /* Peek next state in queue to prepare next state */
            while (m_StatesQueue.Count > 0)
            {
                var (_, nextState, _, tcs) = m_StatesQueue.Peek();
                if (!SetNextState(nextState))
                {
                    tcs.TrySetResult();
                    tcs.TryReturn();
                    /* Remove from queue */
                    m_StatesQueue.Dequeue();
                }
                else
                {
                    break;
                }
            }

            return (completeTransitionFromPrevStateTask, nextId);
        }

        /// <summary>
        /// Returns true if the state with specified id is active and state machine hasn't next state.
        /// You can use this check to ensure that it's safe to push some next state right after transition with provided id (e.g. push Idle state if you wish to cancel previous state with it)
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool IsLatest(ulong id)
        {
            return activeId == id && !m_HasNextState;
        }

        /// <summary>
        /// Returns true if the state with specified id is in queue or active
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool HasState(ulong id)
        {
            return activeId == id || m_StatesQueue.Any(x => x.id == id);
        }

        /// <summary>
        /// Wait for the state with specified id
        /// </summary>
        /// <param name="id"></param>
        /// <param name="throwIfNotFound"></param>
        /// <returns></returns>
        public UniTask WaitState(ulong id, bool throwIfNotFound = false)
        {
            if (activeId == id)
            {
                return completeTransitionFromPrevStateTask;
            }

            /* Search in queue */
            foreach (var (stateId, _, _, tcs) in m_StatesQueue)
            {
                if (stateId == id)
                {
                    return tcs.Task;
                }
            }

            if (throwIfNotFound)
            {
                return UniTask.FromException(new InvalidOperationException($"State with id {id} not found"));
            }

            /* Not found */
            return UniTask.CompletedTask;
        }

        private void EnqueueState(ulong id, TState state, CancellationToken externalCancellationToken,
            ReusableUniTaskCompletionSource tcs)
        {
            if (m_SettingKeepOnlyLatestState && m_StatesQueue.Count > 1)
            {
                /* Remove all states except the last one */
                while (m_StatesQueue.Count > 1)
                {
                    var deqState = m_StatesQueue.Dequeue();
                    deqState.tcs.TrySetResult();
                    deqState.tcs.TryReturn();

                    /* Dispose state */
                    if (m_StateIsDisposable)
                    {
#if DEBUG
                        try
                        {
                            (state as IDisposable)?.Dispose();
                        }
                        catch (Exception e)
                        {
                            Debug.LogError(
                                $"[ASYNC-STATE-MACHINE] Error while disposing state {state} during enqueue filter: {e.Message}");
                        }
#else
                        (state as IDisposable)?.Dispose();
#endif
                    }
                }
            }

            m_StatesQueue.Enqueue((id, state, externalCancellationToken, tcs));
        }

        private async UniTaskVoid PerformTransition(ulong id)
        {
            activeId = id;
            try
            {
                if (m_SettingDebug)
                    Debug.Log(
                        $"[ASYNC-STATE-MACHINE][TRANSITION] Perform transition #{id} {prevState} -> {currentState}");
#if DEBUG
                SetStatus(TransitionStatus.FromPrevToCurrentProcessing, "PerformTransition");
#else
            SetStatus(TransitionStatus.FromPrevToCurrentProcessing);
#endif
                OnTransitionStep?.Invoke(prevState, currentState);
                await m_Handler(new Transition(this, id));
            }
            catch (OperationCanceledException)
            {
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
                    if (m_Status == TransitionStatus.FromPrevToCurrentProcessing ||
                        m_Status == TransitionStatus.Aborted)
                    {
                        TrySetTransitionFromPrevStateCompleted(id);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Log(
                    $"<color=red>[ASYNC_STATE_MACHINE] Exception in transition</color> #{id} {prevState} -> {currentState} (e: {e}");
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
                if (m_Status == TransitionStatus.Error)
                {
                    if (m_SettingDebug)
                        Debug.Log(
                            $"[WINGS BEHAVIOUR] Transition #{id} <color=red>finalizing after error</color> (prev: {prevState}, current: {currentState}, next: {nextState})");
                }
                else
                {
                    if (m_SettingDebug)
                        Debug.Log(
                            $"[WINGS BEHAVIOUR] Transition #{id} finalizing  (prev: {prevState}, current: {currentState}, next: {nextState})");
                }

                TrySetTransitionFromPrevStateCompleted(id);
                SetTransitionToNextStateCompleted(id);
                if (m_SettingDebug)
                    Debug.Log(
                        $"[WINGS BEHAVIOUR] Transition #{id} finalized  (prev: {prevState}, current: {currentState}, next: {nextState})");
                OnTransitionCompleted?.Invoke();
                SetStatus(TransitionStatus.Idle
#if DEBUG
                    , "Transition finalizer"
#endif
                );
                m_LatestTransitionId = id;
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
                // Fallback to default state
                if (m_SettingAutoCompleteWithDefaultState && m_AutocompleteTransitionId != m_LatestTransitionId)
                {
                    if (m_SettingDebug) Debug.Log($"[ASYNC-STATE-MACHINE] Auto push default state");
                    m_AutocompleteTransitionId = GenerateId();
                    PushStateAsync(m_DefaultState, id: m_AutocompleteTransitionId);
                }

                return;
            }

            var (id, newNextState, externalCancellationToken, tcs) = m_StatesQueue.Dequeue();
            if (!previousIsSuccess)
            {
                tcs.TrySetException(exception);
                tcs.TryReturn();

                /* Throw every next state in queue */
                while (m_StatesQueue.Count > 0)
                {
                    var nextTcs = m_StatesQueue.Dequeue().tcs;
                    nextTcs.TrySetException(exception); // TODO: Cover this moment with unit tests
                    nextTcs.TryReturn();
                }

                return;
            }

            try
            {
                var taskToWait = PushStateAsync(newNextState, externalCancellationToken, id).task;
                await taskToWait;
                tcs.TrySetResult();
            }
            catch (Exception e)
            {
                tcs.TrySetException(e);
            }
            finally
            {
                tcs.TryReturn();
            }
        }

        public DisposableAction SubscribeTransitionCompleted(Action onStateCompleted)
        {
            OnTransitionCompleted += onStateCompleted;
            return new DisposableAction(() => OnTransitionCompleted -= onStateCompleted);
        }

        public DisposableAction SubscribeTransition(TransitionHandler onTransition)
        {
            OnTransitionStep += onTransition;
            onTransition(prevState, currentState);
            return new DisposableAction(() => OnTransitionStep -= onTransition);
        }

        public DisposableAction SubscribeStateChange(Action<TState> onStateChange)
        {
            OnStateChange += onStateChange;
            onStateChange(currentState);
            return new DisposableAction(() => OnStateChange -= onStateChange);
        }

        private void Dispose(bool isAsync)
        {
#if DEBUG
            m_Debug_UILoggerDisposable.Dispose();
#endif
            m_Disposed = true;
            m_InternalCancellationTokenSource.Cancel();
            m_InternalCancellationTokenSource.Dispose();

            /* Dispose active states */
            if (m_StateIsDisposable)
            {
#if DEBUG
                try
                {
                    (prevState as IDisposable)?.Dispose();
                    (currentState as IDisposable)?.Dispose();
                    (nextState as IDisposable)?.Dispose();

                    /* Dispose states in queue */
                    foreach (var (_, state, _, tcs) in m_StatesQueue)
                    {
                        tcs.TrySetCanceled(); // TODO: Cancel or Resolve?
                        tcs.TryReturn();
                        (state as IDisposable)?.Dispose();
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("Error while disposing states during state machine disposing: " + e.Message);
                }

#else
                (prevState as IDisposable)?.Dispose();
                (currentState as IDisposable)?.Dispose();
                (nextState as IDisposable)?.Dispose();
                
                /* Dispose states in queue */
                foreach (var (_, state, _, tcs) in m_StatesQueue)
                    {
                        tcs.TrySetCanceled(); // TODO: Cancel or Resolve?
                        tcs.TryReturn();
                        (state as IDisposable)?.Dispose();
                    }
#endif
            }

            if (!isAsync)
            {
                m_TransitionFromPrevStateResolved = true;
                m_TransitionToNextStateResolved = true;
                m_TransitionFromPrevStateCompletionSource?.TrySetCanceled();
                m_TransitionToNextStateCompletionSource?.TrySetCanceled();
                m_TransitionFromPrevStateCompletionSource?.TryReturn();
                m_TransitionToNextStateCompletionSource?.TryReturn();
            }

            GC.SuppressFinalize(this);
        }

        public void Dispose()
        {
            Dispose(false);
        }

        public async UniTask DisposeAsync()
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
            finally
            {
                m_TransitionFromPrevStateResolved = true;
                m_TransitionToNextStateResolved = true;
                m_TransitionFromPrevStateCompletionSource?.TrySetCanceled();
                m_TransitionToNextStateCompletionSource?.TrySetCanceled();
                m_TransitionFromPrevStateCompletionSource?.TryReturn();
                m_TransitionToNextStateCompletionSource?.TryReturn();
            }
            // TODO: Consider do not throw exception of current transition
        }
    }
}