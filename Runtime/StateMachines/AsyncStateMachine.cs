using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Cysharp.Threading.Tasks;
using Metamatic.DevTools;
using Metamatic.Exceptions;
using Debug = UnityEngine.Debug;

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
            public readonly Signal cancellationSignal; // TODO: rename to CapitalCase
            public readonly short ID;

            public TransitionStatus status => m_StateMachine.m_Status;
            public Signal externalCancellationSignal => m_StateMachine.m_ExternalCancellationSignal;
            public TState prevState => m_StateMachine.prevState;
            public TState state => m_StateMachine.currentState;
            public TState nextState => m_StateMachine.nextState;
            public bool hasNextState => m_StateMachine.m_HasNextState;

            public bool isExternalCancellationSignalCancelled =>
                m_StateMachine.m_ExternalCancellationSignal.IsCancellationRequested;

            public UniTask completeTransitionFromPrevStateTask => m_StateMachine.completeTransitionFromPrevStateTask;
            public UniTask completeTransitionToNextStateTask => m_StateMachine.completeTransitionToNextStateTask;
            public bool isInitial => m_StateMachine.isInitial;
            public bool isFinalize => m_StateMachine.m_Status == TransitionStatus.Aborted;
            public Signal generalCancellationSignal => m_StateMachine.internalCancellationSignal;
            public bool isAborted => m_StateMachine.m_Status == TransitionStatus.Aborted;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public UniTask<TState> WaitNextState() => m_StateMachine.SetTransitionReadyAndWaitNextState();

            public DisposableAction AddNextStateValidator(Validator<TState, TState>.ValidatorDelegate validator)
            {
                return m_StateMachine.AddNextStateValidator(validator);
            }

            internal Transition(AsyncStateMachine<TState> stateMachine, short id)
            {
                m_StateMachine = stateMachine;
                cancellationSignal = stateMachine.cancellationSignal;
                ID = id;
            }
        }

        private readonly bool m_StateIsDisposable;

        private TransitionStatus m_Status = TransitionStatus.Initial;

        /// <summary>
        /// Internal cancellation token for the whole state machine
        /// </summary>
        private SignalSource m_InternalCancellationSignalSource = SignalSource.Create();

        /// <summary>
        /// External cancellation token passed within the state
        /// </summary>
        private Signal m_ExternalCancellationSignal;

        /// <summary>
        /// Cancellation token source will be canceled when the transition gets next state, thus transition handler can know that it should stop
        /// </summary>
        private SignalSource m_CancellationSignalSource;

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
        private bool m_TransitionFromPrevStateResolved;

        /// <summary>
        /// Indicated that transition from current state to next state is resolved (to avoid using return value of TrySetResult)
        /// </summary>
        private bool m_TransitionToNextStateResolved;

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
        private short m_LastId;

        /// <summary>
        /// State ID that is currently active
        /// </summary>
        private short activeId { get; set; }

        /// <summary>
        /// Determines that external cancellation token is canceled (thus SetState should throw OperationCanceledException)
        /// </summary>
        private bool isExternalCancellationSignalCancelled => m_ExternalCancellationSignal.IsCancellationRequested;

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
        /// CancellationSignal for the transition (will be canceled when the transition gets next state)
        /// </summary>
        private Signal cancellationSignal => m_CancellationSignalSource.Signal;

        /// <summary>
        /// Internal cancellation token for the whole state machine (will be canceled when state machine is disposed)
        /// </summary>
        public Signal internalCancellationSignal => m_InternalCancellationSignalSource.Signal;

        private UniTask task =>
            UniTask.WhenAll(m_TransitionFromPrevStateCompletionSource?.Task ?? UniTask.CompletedTask,
                m_TransitionToNextStateCompletionSource?.Task ?? UniTask.CompletedTask);

        public event Action<TState> OnStateChange;

        private void Reset(TState nextStateForReset, TState nextPrevState = default,
            Signal externalCancellationSignal = default)
        {
            if (m_SettingDebug)
                Console.Verbose("AsyncStateMachine",$"Reset (new prevState: {nextPrevState}, nextState: {nextStateForReset}");
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
                Console.Verbose("AsyncStateMachine",
                    $"Error while disposing prev state {prevState} during Reset to next state {nextStateForReset}: {e.Message}");
            }
#else
            /* Dispose prev state if it supports IDisposable */
            if (m_StateIsDisposable && m_HasPrevState)
            {
                (prevState as IDisposable)?.Dispose();
            }
#endif

            currentState = nextStateForReset;
            OnStateChange?.Invoke(nextStateForReset);
            prevState = nextPrevState;
            m_ExternalCancellationSignal = externalCancellationSignal;
            if (m_SettingDebug)
            {
                Console.Verbose("AsyncStateMachine", "Reset", $"Reset state machine to {currentState}");
            }
            m_CancellationSignalSource.Dispose();
            m_CancellationSignalSource =
                SignalSource.CreateLinked(m_InternalCancellationSignalSource.Signal,
                    externalCancellationSignal);
            
            if (m_TransitionFromPrevStateCompletionSource == null)
            {
                m_TransitionFromPrevStateCompletionSource = ReusableUniTaskCompletionSource.Create();
                #if DEBUG
                if (m_SettingDebug)
                    Console.Verbose("AsyncStateMachine", "Transition", $"Init transition from prev state completion source #{m_TransitionFromPrevStateCompletionSource.debugVersion}");
                #endif
            }
            else
            {
                m_TransitionFromPrevStateCompletionSource.Reset();
#if DEBUG
                if (m_SettingDebug)
                    Console.Verbose("AsyncStateMachine", "Transition", $"Reset transition from prev state completion source #{m_TransitionFromPrevStateCompletionSource.debugVersion}");
#endif
            }
            
            if (m_TransitionToNextStateCompletionSource == null)
            {
                m_TransitionToNextStateCompletionSource = ReusableUniTaskCompletionSource.Create();
#if DEBUG
                if (m_SettingDebug)
                    Console.Verbose("AsyncStateMachine", "Transition",$"Init transition from current state to next state completion source #{m_TransitionToNextStateCompletionSource.debugVersion}");
#endif
            }
            else
            {
                m_TransitionToNextStateCompletionSource.Reset();
#if DEBUG
                if (m_SettingDebug)
                    Console.Verbose("AsyncStateMachine", "Transition", $"Reset transition from current state to next state completion source #{m_TransitionToNextStateCompletionSource.debugVersion}");
#endif
            }
            m_TransitionFromPrevStateResolved = false;
            m_TransitionToNextStateResolved = false;
            m_Status = TransitionStatus.Idle;
            m_HasNextState = false;
            m_HasPrevState = true;
            nextState = default;
            OnNextState = null;
            OnAborted = null;
        }

        private void Reset(TState nextStateForReset, Signal externalCancellationSignal = default)
        {
            Reset(nextStateForReset, default, externalCancellationSignal);
            prevState = m_DefaultState;
            m_HasPrevState = false;
        }

        /// <summary>
        /// Add validation function that validates every next state. If it returns false, the state will be ignored. You can add multiple validators, all of them should return true to pass the validation. Don't forget to dispose the returned object to remove the validator.
        /// </summary>
        /// <param name="validator"></param>
        /// <returns></returns>
        public DisposableAction AddNextStateValidator(Validator<TState, TState>.ValidatorDelegate validator)
        {
            return m_NextStateValidator.Add(validator);
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
            m_Status = status;
#if DEBUG
            if (m_SettingDebug)
                Console.Verbose("AsyncStateMachine", 
                    $"Set status {status} (prev: {prevState}, current: {currentState}, next: {nextState}) Source: {source}");
            Debug_SetTransitionStatus(status, source);
#endif
        }

        /// <summary>
        /// Method to wait for the next state (should be used by transition handler when it is ready to transition to the next state)
        /// </summary>
        /// <returns></returns>
        public UniTask<TState> SetTransitionReadyAndWaitNextState()
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
                Enumerable.Select(m_TransitionStatusQueue.originalQueue, x => $"{x.Item1} ({x.Item2})"));
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
            void OnNextStateHandler(TState newNextState)
            {
                OnNextState -= OnNextStateHandler;
                OnAborted -= OnAbortedHandler;
                if (m_SettingDebug)
                {
                    Console.Verbose("AsyncStateMachine", $"Cancel because next state is defined during awaiting next state: {newNextState}");
                }
                m_CancellationSignalSource.TryRaise();
                tsc.TrySetResult(newNextState);
            }

            void OnAbortedHandler()
            {
                OnNextState -= OnNextStateHandler;
                OnAborted -= OnAbortedHandler;
                tsc.TrySetCanceled();
            }

            OnNextState += OnNextStateHandler;
            OnAborted += OnAbortedHandler;

            TrySetTransitionFromPrevStateCompleted(0);

            return tsc.Task;
        }
        
        #if DEBUG
        private StackTrace m_PreviousSetNextStateStackTrace;
        #endif

        private bool SetNextState(TState newNextState, bool skipValidationCheck = false)
        {
            #if DEBUG
            if (m_HasNextState) Debug.LogError($"Already has next state. Current state: {currentState}, next state: {nextState}. Attempt to set next state: {newNextState}. Previous call stack trace: {m_PreviousSetNextStateStackTrace}");
            if (m_SettingDebug) Console.Verbose("AsyncStateMachine", $"Set next state: {newNextState}");
            m_PreviousSetNextStateStackTrace = new StackTrace();
            #endif
            
            /* Invalidate state */
            if (!skipValidationCheck && !m_NextStateValidator.Validate(state, newNextState))
            {
                /* As the state is invalid, we should dispose it if it supports IDisposable */
                if (m_StateIsDisposable)
                {
                    (newNextState as IDisposable)?.Dispose();
                }

                return false;
            }

            nextState = newNextState;
            m_HasNextState = true;

            if (m_Status == TransitionStatus.FromPrevToCurrentCompleted)
            {
#if DEBUG
                SetStatus(TransitionStatus.FromCurrentToNextProcessing, "SetNextState");
#else
                SetStatus(TransitionStatus.FromCurrentToNextProcessing);
#endif
            }

            var memoizedCancellationSignalSource = m_CancellationSignalSource;
            OnNextState?.Invoke(newNextState);
#if DEBUG
            if (m_SettingDebug)
            {
                Console.Verbose("AsyncStateMachine", $"Cancel because next state is defined: {newNextState}");
            }
#endif
            memoizedCancellationSignalSource.TryRaise();
            
            return true;
        }

        private void Abort()
        {
#if DEBUG
            SetStatus(TransitionStatus.Aborted, "Abort");
#else
            SetStatus(TransitionStatus.Aborted);
#endif
            
            OnAborted?.Invoke();
        }

        public void SetPrevState(TState newPrevState)
        {
            prevState = newPrevState;
        }

        private void TrySetTransitionFromPrevStateCompleted(short transitionId)
        {
            if (!m_TransitionFromPrevStateResolved)
            {
                if (m_SettingDebug)
                    Console.Verbose("AsyncStateMachine",$"Complete transition #{transitionId} from prev to current");
#if DEBUG
                SetStatus(TransitionStatus.FromPrevToCurrentCompleted, "SetTransitionFromPrevStateCompleted");
#else
            SetStatus(TransitionStatus.FromPrevToCurrentCompleted);
#endif
                m_TransitionFromPrevStateResolved = true;
                m_TransitionFromPrevStateCompletionSource.TrySetResult();
            }
        }

        private void SetTransitionToNextStateCompleted(short transitionId)
        {
            if (!m_TransitionToNextStateResolved)
            {
                if (m_SettingDebug)
                    Console.Verbose("AsyncStateMachine", $"Complete transition #{transitionId} from current to next");
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
        private bool m_RefuseNextStateIfNotReady;
        private bool m_SettingAutoCompleteWithDefaultState;
        private bool m_SettingDebug;

        /// <summary>
        /// Determines that state machine is autocompleted with default state (according to m_SettingAutoCompleteWithDefaultState). This property is needed to avoid adding default state to the queue when it is already added that can cause infinite loop
        /// </summary>
        private short m_LatestTransitionId;

        private short m_AutocompleteTransitionId;

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
        /// <param name="debug">Enable debug logging</param>
        /// <param name="refuseNextStateIfNotReady">Ignore adding next state if the current transition is not ready</param>
        public AsyncStateMachine(StateChangeHandler handler, TState defaultState = default,
            bool keepOnlyLatestState = false, bool autoCompleteWithDefaultState = false, bool debug = false, bool refuseNextStateIfNotReady = false)
        {
            m_Handler = handler;
            m_DefaultState = defaultState;
            m_SettingKeepOnlyLatestState = keepOnlyLatestState;
            m_SettingAutoCompleteWithDefaultState = autoCompleteWithDefaultState;
            m_SettingDebug = debug;
            m_RefuseNextStateIfNotReady = refuseNextStateIfNotReady;
            currentState = defaultState;
            m_StateIsDisposable = typeof(IDisposable).IsAssignableFrom(typeof(TState));

#if DEBUG
            if (m_SettingDebug)
            {
                // TODO: do we need this?
                m_NextStateValidator.debug = true;
                m_Debug_UILoggerDisposable = Monitoring.Create("Async state machine", "Validator",
                    () => m_NextStateValidator.ToString());
            }
#endif
        }

        ~AsyncStateMachine()
        {
            Console.Exception("AsyncStateMachine", "AsyncStateMachine should be disposed manually");
        }

        private short GenerateId()
        {
            return ++m_LastId == short.MaxValue ? m_LastId = 1 : m_LastId;
        }

        private Queue<(short id, TState state, Signal cancellationToken, ReusableUniTaskCompletionSource tcs)>
            m_StatesQueue =
                new();

        /// <summary>
        /// Add new state to the queue
        /// </summary>
        /// <param name="stateToPush"></param>
        /// <param name="externalCancellationSignal"></param>
        /// <param name="id"></param>
        /// <returns>Uni task for transition between prev task to this task</returns>
        public UniTask PushLegacy(TState stateToPush, Signal externalCancellationSignal = default, short id = 0)
        {
            var transitionId = PushState(stateToPush, externalCancellationSignal, id);
            return WaitState(transitionId);
        }

        /// <summary>
        /// Add next state to the queue if current state matches the specified state
        /// </summary>
        /// <param name="ifCurrentStateIs"></param>
        /// <param name="newNextState"></param>
        /// <param name="externalCancellationSignal"></param>
        /// <returns></returns>
        public UniTask TryPushNextState(TState ifCurrentStateIs, TState newNextState,
            Signal externalCancellationSignal = default)
        {
            if (currentState.Equals(ifCurrentStateIs) && m_StatesQueue.Count == 0)
            {
                return PushLegacy(newNextState, externalCancellationSignal);
            }

            return UniTask.CompletedTask;
        }
        
        
        /// <summary>
        /// Push next state only if the current state is not the same as the specified state
        /// </summary>
        /// <param name="newNextState"></param>
        /// <param name="externalCancellationSignal"></param>
        public bool TryPushState(TState newNextState, Signal externalCancellationSignal = default)
        {
            if (!currentState.Equals(newNextState))
            {
                PushStateLegacy(newNextState, externalCancellationSignal);
                return true;
            }

            return false;
        }

        /// <summary>
        /// This method allows you to push next state right after transition with specified ID
        /// E.g.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="newNextState"></param>
        /// <param name="signal"></param>
        /// <returns></returns>
        public short TryPushNextState(short id, TState newNextState, Signal signal = default)
        {
            if (IsLatest(id))
            {
                return PushStateLegacy(newNextState, signal);
            }

            return 0;
        }

        /// <summary>
        /// More convenient method to add state to the queue and get its id
        /// In the future versions this method can stay as the only method to add state, because its interface is more convenient to reduce the number of created UniTaskCompletionSources per flow of the state machine
        /// </summary>
        /// <param name="newState"></param>
        /// <param name="externalCancellationSignal"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public short PushStateLegacy(TState newState, Signal externalCancellationSignal = default, short id = 0)
        {
            return PushState(newState, externalCancellationSignal, id);
        }
        
        public UniTask PushStateAsync(TState newState, Signal externalCancellationSignal = default, short id = 0)
        {
            return WaitState(PushState(newState, externalCancellationSignal, id));
        }

        /// <summary>
        /// Add new state to the queue and get its task and id
        /// </summary>
        /// <param name="newState"></param>
        /// <param name="externalCancellationSignal"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public short PushState(TState newState,
            Signal externalCancellationSignal = default,
            short id = 0)
        {
#if DEBUG
            if (m_SettingDebug)
                Console.Verbose("AsyncStateMachine", id  == 0 ? "Push" : "Dequeue",
                    $"Attempt to add state {newState} | (id #{id}) (Curren status is {m_Status}). Stack trace: {Environment.StackTrace}");
#endif
            /* Invalidate state */
            if (m_Disposed || !m_NextStateValidator.Validate(currentState, newState) || (m_RefuseNextStateIfNotReady && m_Status == TransitionStatus.FromPrevToCurrentProcessing))
            {
                /* As the state is invalid, we should dispose it if it supports IDisposable */
                if (m_StateIsDisposable)
                {
                    try
                    {
                        (newState as IDisposable)?.Dispose();
                    }
                    catch (Exception e)
                    {
                        Console.Exception("AsyncStateMachine", e.Message, e.StackTrace);
                    }
                }
                return 0;
            }
#if DEBUG
            if (m_SettingDebug)
                Console.Verbose("AsyncStateMachine", "Push", $"{newState} (id #{id}) (Curren status is {m_Status})");
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
                        /* But first we should check if queue already has next state */
                        var tcs = ReusableUniTaskCompletionSource.Create();
                        #if DEBUG
                        if (m_SettingDebug)
                            Console.Verbose("AsyncStateMachine", "Task", $"Init task completion source #{tcs.debugVersion} for {newState}(id #{id}) (Curren status is {m_Status})");
                        #endif
                        var isFirstInQueue = m_StatesQueue.Count == 0;
                        EnqueueState(nextId, newState, externalCancellationSignal, tcs);
                        if (isFirstInQueue)
                        {
                            // Do not need to check validation handler, because it was checked before
                            SetNextState(newState, true);
                        }

                        return nextId;
                    }
                    case TransitionStatus.FromCurrentToNextProcessing:
                    case TransitionStatus.FromCurrentToNextCompleted:
                    case TransitionStatus.Error:
                    {
                        /* Transition is in state of transitioning between current state and next state. We should add this state to queue */
                        var tcs = ReusableUniTaskCompletionSource.Create();
#if DEBUG
                        if (m_SettingDebug)
                            Console.Verbose("AsyncStateMachine", "Task", $"Init task completion source #{tcs.debugVersion} for {newState}(id #{id}) (Curren status is {m_Status})");
#endif
                        EnqueueState(nextId, newState, externalCancellationSignal, tcs);
                        return nextId;
                    }
                        /* No transition is in progress, therefore we can set next state immediately */
                    case TransitionStatus.Idle:
                        Reset(newState, currentState, externalCancellationSignal);
                    break;
                    default:
                        throw new InvalidOperationException(
                            $"Invalid state {m_Status} for adding new state {newState} (id #{id})");
                        break;
                }
            }
            else
            {
                Reset(newState, externalCancellationSignal);
            }

            /* Create new transition and perform */
            PerformTransition(nextId).Forget();
            /* Peek next state in queue to prepare next state */
            while (m_StatesQueue.Count > 0)
            {
                var (_, nextStateFromQueue, _, tcs) = m_StatesQueue.Peek();
                if (!SetNextState(nextStateFromQueue))
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

            return nextId;
        }

        /// <summary>
        /// Returns true if the state with specified id is active and state machine hasn't next state.
        /// You can use this check to ensure that it's safe to push some next state right after transition with provided id (e.g. push Idle state if you wish to cancel previous state with it)
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool IsLatest(short id)
        {
            return activeId == id && !m_HasNextState;
        }

        /// <summary>
        /// Returns true if the state with specified id is in queue or active
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool HasState(short id)
        {
            if (id == 0) return false;
            return activeId == id || m_StatesQueue.Any(x => x.id == id);
        }

        /// <summary>
        /// Returns true if the state with specified id is in queue or active
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool isValidState(short id)
        {
            if (id == 0) return false;
            return activeId == id || m_StatesQueue.Any(x => x.id == id);
        }
        
        /**
         * Get state observer for the state with specified id. This structure can be used to wait for the state with specified id.
         * Unlike UniTask, this structure doest not create new tasks in advance. Thus, it is not mandatory to wait.
         * This is a safe way to provide the option to wait for the transition to complete when waiting may be optional.
         */
        public LowCouplingTask GetStateObserver(short id)
        {
            return new LowCouplingTask(new Token96Bit(id, -1), WaitState);
        }
        
        /**
         * Wait for current state ready
         */
        public UniTask WaitCurrentState(Signal cancellationSignal = default)
        {
            if (activeId == 0 || m_Status != TransitionStatus.FromPrevToCurrentProcessing)
            {
                return UniTask.CompletedTask;
            }

            if (cancellationSignal.CanBeCanceled)
            {
                var cancellationTask = cancellationSignal.ToUniTask(true);
                return UniTask.WhenAny(completeTransitionFromPrevStateTask, cancellationTask);
            }
            return completeTransitionFromPrevStateTask;
        }

        /// <summary>
        /// Wait for the state with specified id
        /// </summary>
        /// <param name="id"></param>
        /// <param name="throwIfNotFound"></param>
        /// <returns></returns>
        public UniTask WaitState(short id, bool throwIfNotFound = false, Signal cancellationSignal = default)
        {
            if (activeId == id)
            {
                if (cancellationSignal.CanBeCanceled)
                {
                    var cancellationTask = cancellationSignal.ToUniTask(true);
                    return UniTask.WhenAny(completeTransitionFromPrevStateTask, cancellationTask);
                }
                return completeTransitionFromPrevStateTask;
            }

            /* Search in queue */
            foreach (var (stateId, _, _, tcs) in m_StatesQueue)
            {
                if (stateId == id)
                {
                    if (cancellationSignal.CanBeCanceled)
                    {
                        var cancellationTask = cancellationSignal.ToUniTask(true);
                        return UniTask.WhenAny(tcs.Task, cancellationTask);
                    }
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
        
        public UniTask WaitState(Token96Bit token, bool throwIfNotFound = false, Signal cancellationSignal = default)
        {
            return WaitState((short)token.ID, throwIfNotFound, cancellationSignal);
        }

        private void EnqueueState(short id, TState stateToEnqueue, Signal externalCancellationSignal,
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
                            (stateToEnqueue as IDisposable)?.Dispose();
                        }
                        catch (Exception e)
                        {
                            Debug.LogError(
                                $"[ASYNC-STATE-MACHINE] Error while disposing state {stateToEnqueue} during enqueue filter: {e.Message}");
                        }
#else
                        (state as IDisposable)?.Dispose();
#endif
                    }
                }
            }

            m_StatesQueue.Enqueue((id, stateToEnqueue, externalCancellationSignal, tcs));
        }

        private async UniTaskVoid PerformTransition(short id)
        {
            activeId = id;
            try
            {
                if (m_SettingDebug)
                    Console.Verbose("AsyncStateMachine", "Transition", "Start",
                        $"Perform transition #{id} {prevState} -> {currentState}");
#if DEBUG
                SetStatus(TransitionStatus.FromPrevToCurrentProcessing, "PerformTransition");
#else
            SetStatus(TransitionStatus.FromPrevToCurrentProcessing);
#endif
                
                #if !DEBUG
                // Wait next frame to ensure that next state are set correctly from the m_StatesQueue)
                await UniTask.Yield();
                #endif
                OnTransitionStep?.Invoke(prevState, currentState);
                await m_Handler(new Transition(this, id));
                if (m_SettingDebug)
                    Console.Verbose("AsyncStateMachine", "Transition", 
                        $"Transition #{id} {prevState} -> {currentState} completed without errors");
            }
            catch (OperationCanceledException)
            {
                // Skip
                if (isExternalCancellationSignalCancelled &&
                    m_Status == TransitionStatus.FromPrevToCurrentProcessing)
                {
                    if (m_SettingDebug)
                        Console.Verbose("AsyncStateMachine", "Transition", 
                            $"Cancel transition #{id} by external token (prev: {prevState}, current: {currentState})");
                    /* Transition was cancelled by external token. Throw this token */
                    SetTransitionFromPrevStateError(
                        new OperationCanceledException(m_ExternalCancellationSignal));
                }
                else
                {
                    /* Otherwise, it was cancelled by the transition itself, thus it should be considered as normal behaviour. Resolve it. */
                    if (m_Status == TransitionStatus.FromPrevToCurrentProcessing ||
                        m_Status == TransitionStatus.Aborted)
                    {
                        if (m_SettingDebug)
                            Console.Verbose("AsyncStateMachine", "Transition", 
                                $"Transition #{id} was cancelled at status {m_Status}");
                        TrySetTransitionFromPrevStateCompleted(id);
                    }
                    else
                    {
                        Console.Exception("AsyncStateMachine",
                            $"Transition #{id} was cancelled, but it is not in the correct state to resolve it. Current status: {m_Status}");
                    }
                }
            }
            catch (Exception e)
            {
                global::Metamatic.Console.Exception("AsyncStateMachine",
                    $"Exception in transition</color> #{id} {prevState} -> {currentState} (e: {e}");
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
                if (m_SettingDebug)
                    Console.Verbose("AsyncStateMachine", "Transition", "Finally",
                        $"Transition #{id} enter finally block  (prev: {prevState}, current: {currentState}, next: {nextState})");
                if (m_Status == TransitionStatus.Error)
                {
                    if (m_SettingDebug)
                        Console.Verbose("AsyncStateMachine", "Transition",
                            $"Transition #{id} <color=red>finalizing after error</color> (prev: {prevState}, current: {currentState}, next: {nextState})");
                }

                TrySetTransitionFromPrevStateCompleted(id);
                SetTransitionToNextStateCompleted(id);
                OnTransitionCompleted?.Invoke();
                SetStatus(TransitionStatus.Idle
#if DEBUG
                    , "Transition finalizer"
#endif
                );
                m_LatestTransitionId = id;
                DeferCallNextStateOnCurrentTransitionReady().Forget();
            }
        }

        /// <summary>
        /// When new transition is performing, we should wait for it's readiness to apply next state (if it is in queue)
        /// </summary>
        private async UniTaskVoid DeferCallNextStateOnCurrentTransitionReady()
        {
            try
            {
                await completeTransitionToNextStateTask;
                HandleNextState(true).Forget();
            }
            catch (OperationCanceledException)
            {
                /* Transition to next state was cancelled, but it is not a problem, because it is expected behaviour */
                HandleNextState(true).Forget();
            }
            catch (Exception e)
            {
                HandleNextState(false, e).Forget();
            }
        }

        private async UniTaskVoid HandleNextState(bool previousIsSuccess, Exception exception = null)
        {
            if (m_StatesQueue.Count == 0)
            {
                // Fallback to default state if there are no states in queue
                if (m_SettingAutoCompleteWithDefaultState && m_AutocompleteTransitionId != m_LatestTransitionId)
                {
                    if (m_SettingDebug) Console.Verbose("AsyncStateMachine", $"Auto push default state");
                    m_AutocompleteTransitionId = GenerateId();
                    PushState(m_DefaultState, id: m_AutocompleteTransitionId);
                }

                return;
            }

            var (id, newNextState, externalCancellationSignal, tcs) = m_StatesQueue.Dequeue();
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
                var taskToWait = WaitState(PushState(newNextState, externalCancellationSignal, id));
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
            m_CancellationSignalSource.Dispose();
            m_InternalCancellationSignalSource.Dispose();

            /* Dispose active states */
            if (m_StateIsDisposable)
            {
                try
                {
                    (prevState as IDisposable)?.Dispose();
                    (currentState as IDisposable)?.Dispose();
                    (nextState as IDisposable)?.Dispose();

                    /* Dispose states in queue */
                    foreach (var (_, stateFromQueue, _, tcs) in m_StatesQueue)
                    {
                        tcs.TrySetCanceled(); // TODO: Cancel or Resolve?
                        tcs.TryReturn();
                        (stateFromQueue as IDisposable)?.Dispose();
                    }
                }
                catch (Exception e)
                {
                    Console.Exception(e.Message, e.StackTrace);
                }
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