using System;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    /// <summary>
    /// Async cancellation acts as a token that can be used to asynchronously cancel an async operation. I supports abortable async cancellation logic,
    /// which means that the cancellation can be requested asynchronously, but during the disposing process, it can be aborted (disposed or cancelled synchronously).
    /// </summary>
    public readonly struct AsyncCancellation: IAbortable
    {
        // ReSharper disable once InconsistentNaming
        public static AsyncCancellation Cancelled => new AsyncCancellation(null);
        private readonly CancellationSourceImpl m_Impl;
        private readonly int m_Version;
        
        public bool isAlive => GetImpl() != null;
        
        public enum State
        {
            Idle,
            CancelledSync,
            CancelledAsync
        }
        
        /// <summary>
        /// This signal is triggered when the cancellation is requested (no matter if it is sync or async)
        /// </summary>
        // ReSharper disable once InconsistentNaming
        public Signal Signal => GetImpl()?.signal ?? Signal.Raised;
        /// <summary>
        /// This signal is triggered when the cancellation was already requested asynchronously, but during the disposing process, it was aborted (disposed or cancelled synchronously)
        /// There's no point to get an abort signal if the cancellation is not requested. Moreover, accessing this signal before the async cancellation is requested will return Signal.None.
        /// </summary>
        // ReSharper disable once InconsistentNaming
        public Signal AbortSignal => GetImpl()?.abortSignal ?? Signal.Raised;

        public bool isCancellationRequested
        {
            get
            {
                var impl = GetImpl();
                return impl == null || impl.state != State.Idle;
            }
        }
        
        public bool isCancelledAsync
        {
            get
            {
                var impl = GetImpl();
                return impl is { state: State.CancelledAsync };
            }
        }
        
        public void ThrowIfCancellationRequested()
        {
            if (isCancellationRequested)
            {
                throw new OperationCanceledException();
            }
        }
        
        public AsyncCancellation(CancellationSourceImpl impl)
        {
            m_Impl = impl;
            m_Version = impl?.version ?? -1;
        }
        
        private CancellationSourceImpl GetImpl()
        {
            return m_Impl?.version == m_Version ? m_Impl : null;
        }
        
        /// <summary>
        /// Allows to subscribe to the cancellation event. The handler can be called only once. First parameter indicates whether the cancellation is requested synchronously or not.
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        public DisposableAction Register(Action<bool> handler)
        {
            var impl = GetImpl();
            if (impl != null)
            {
                if (impl.state == AsyncCancellation.State.Idle)
                {
                    var signal = impl.signal;
                    impl.OnCancel += handler;
                    var version = impl.version;
                    return DisposableAction.Create(() =>
                    {
                        if (impl.version == version)
                        {
                            impl.OnCancel -= handler;
                        }
                    });
                } else {
                    handler(impl.state == AsyncCancellation.State.CancelledAsync);
                    return DisposableAction.Empty;
                }
            } else {
                handler(false);
                return DisposableAction.Empty;
            }
        }
        
        /// <summary>
        /// Determines whether the cancellation is requested synchronously or not. If cancellation is requested asynchronously isAborted will always return false.
        /// It also returns true if the cancellation was disposed or returned to the pool.
        /// </summary>
        public bool isAborted => GetImpl() == null;
        /// <summary>
        /// Abort the cancellation. It means that the cancellation is requested synchronously.
        /// </summary>
        public void Abort()
        {
            GetImpl()?.TryCancelSync();
        }
        /// <summary>
        /// Add a listener to the aborted event. The listener will be removed after the first call.
        /// You can use this method to listen to the abort event just like Register method in CancellationToken.
        /// In the case of AsyncCancellation, the abort event is triggered when the cancellation is requested synchronously.
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public DisposableAction AddAbortedListenerOnce(Action action)
        {
            var impl = GetImpl();
            if (impl != null)
            {
                return impl.AddAbortedListenerOnce(action);
            } else {
                action();
                return DisposableAction.Empty;
            }
        }
    }

    public readonly struct AsyncCancellationSource: IDisposable, IAbortable
    {
        private readonly CancellationSourceImpl m_Impl;
        private readonly int m_Version;
        // ReSharper disable once InconsistentNaming
        public AsyncCancellation Cancellation => new AsyncCancellation(GetImpl());
        public bool isAlive => GetImpl() != null;
        public static AsyncCancellationSource Create()
        {
            var impl = CancellationSourceImpl.Get();
            #if DEBUG
            var stackFrame = new System.Diagnostics.StackFrame(1, true);
            impl.DebugCreateStackFrame = stackFrame;
            #endif
            return new AsyncCancellationSource(impl);
        }
        // ReSharper disable once InconsistentNaming
        public static AsyncCancellationSource Aborted => new AsyncCancellationSource(null);
        public void TryCancelSync()
        {
            GetImpl()?.TryCancelSync();
        }
        
        public DisposableAction TryCancelAsync(float timeout = 10f)
        {
            return GetImpl()?.TryCancelAsync(timeout) ?? DisposableAction.Empty;
        }
        
        // ReSharper disable once InconsistentNaming
        public Signal Signal => GetImpl()?.signal ?? Signal.Raised;
        public Signal abortDisposingAsyncSignal => GetImpl()?.abortSignal ?? Signal.Raised;

        private AsyncCancellationSource(CancellationSourceImpl impl)
        {
            m_Impl = impl;
            m_Version = impl?.version ?? -1;
        }

        private CancellationSourceImpl GetImpl()
        {
            return m_Impl?.version == m_Version ? m_Impl : null;
        }

        public void Dispose()
        {
            GetImpl()?.TryCancelSync();
        }
        
        /// <summary>
        /// Add a listener to the reset event. The listener will be removed after the first call.
        /// </summary>
        /// <param name="action"></param>
        public DisposableAction AddResetListenerOnce(Action action)
        {
            var impl = GetImpl();
            if (impl != null)
            {
                impl.OnReset += action;
                return DisposableAction.Create(() =>
                {
                    impl.OnReset -= action;
                });
            } else {
                action();
                return DisposableAction.Empty;
            }
        }
        
        public void ThrowIfCancellationRequested()
        {
            if (GetImpl()?.state != AsyncCancellation.State.Idle)
            {
                throw new OperationCanceledException();
            }
        }

        public bool isAborted => GetImpl() == null;
        public void Abort()
        {
            GetImpl()?.TryCancelSync();
        }

        public DisposableAction AddAbortedListenerOnce(Action action)
        {
            var impl = GetImpl();
            if (impl != null)
            {
                return impl.AddAbortedListenerOnce(action);
            } else {
                action();
                return DisposableAction.Empty;
            }
        }
        
        public DisposableAction AddDisposedListenerOnce(Action<bool> action)
        {
            var impl = GetImpl();
            if (impl != null)
            {
                impl.OnCancel += action;
                
                return DisposableAction.Create(() =>
                {
                    impl.OnCancel -= action;
                });
            } else {
                action(false);
                return DisposableAction.Empty;
            }
        }
    }
    

    public class CancellationSourceImpl: IResettable, IInitializable, IDisposable, IAbortable
    {
        #if DEBUG
        public static readonly ObservableList<CancellationSourceImpl> Instances = new ObservableList<CancellationSourceImpl>();
        public System.Diagnostics.StackFrame DebugCreateStackFrame;
        #endif
        private static ParamLessPool<CancellationSourceImpl> _Pool = new(10); // TODO: Decide how many instances to keep in the pool
        internal static CancellationSourceImpl Get()
        {
            return _Pool.Get();
        }
        
        public CancellationSourceImpl()
        {
            #if DEBUG
            Instances.Add(this);
            #endif
        }
        
        public AsyncCancellation.State state { get; private set; } = AsyncCancellation.State.Idle;
        public int version { get; private set; }
        private SignalSource m_SignalSource = default;
        private SignalSource m_AbortSignalSource = default;
        /// <summary>
        /// This event is triggered when the sync cancellation is requested or when object is returned to the pool, or when it is disposed.
        /// </summary>
        public event Action OnAbort;

        public event Action<bool> OnCancel;
        
        public Signal signal => m_SignalSource.Signal;

        public Signal abortSignal
        {
            get
            {
                switch (state)
                {
                    case AsyncCancellation.State.Idle:
                        return m_AbortSignalSource.Signal;
                    case AsyncCancellation.State.CancelledSync:
                        /*
                         * The signal is raised, because the cancellation is already cancelled synchronously.
                         */
                        return Signal.Raised;
                    case AsyncCancellation.State.CancelledAsync:
                        return m_AbortSignalSource.Signal;
                }
                return Signal.None;
            }
        }
        
        /// <summary>
        /// Force return the instance to the pool
        /// </summary>
        private void Return()
        {
            _Pool.Return(this); // Reset is called automatically
        }
        
        public void TryCancelSync()
        {
            switch (state)
            {
                case AsyncCancellation.State.Idle:
                    state = AsyncCancellation.State.CancelledSync;
                    OnCancel?.Invoke(false);
                    m_SignalSource.TryRaise();
                    m_AbortSignalSource.TryRaise();
                    Return(); // After sync cancel, we can return the instance to the pool, because it is not needed anymore.
                    break;
                case AsyncCancellation.State.CancelledSync:
                    // Already cancelled sync. Ignore.
                    break;
                case AsyncCancellation.State.CancelledAsync:
                    state = AsyncCancellation.State.CancelledSync;
                    m_AbortSignalSource.TryRaise();
                    Return(); // After sync cancel, we can return the instance to the pool, because it is not needed anymore.
                    break;
            }
        }
        
        /// <summary>
        /// Raise the async cancellation signal. This method is used to cancel the async operation.
        /// </summary>
        /// <param name="timeout">Determines the timout of automatically sync dispose. It is required because any async cancelled signal should be returned to the pool.</param>
        public DisposableAction TryCancelAsync(float timeout = 10f)
        {
            switch (state)
            {
                case AsyncCancellation.State.Idle:
                    state = AsyncCancellation.State.CancelledAsync;
                    m_AbortSignalSource = SignalSource.Create();
                    OnCancel?.Invoke(true);
                    m_SignalSource.TryRaise();
                    if (timeout > 0f)
                    {
                        StartAutoCancelSyncTimer(timeout);
                    }
                    else
                    {
                        return DisposableAction.Create(Abort);
                    }
                    break;
                case AsyncCancellation.State.CancelledSync:
                    // Already cancelled sync. Unable to cancel async.
                    break;
                case AsyncCancellation.State.CancelledAsync:
                    // Already cancelled async. Ignore.
                    break;
            }
            return DisposableAction.Empty;
        }
        
        private void StartAutoCancelSyncTimer(float timeout)
        {
            if (timeout > 0f)
            {
                var currentVersion = version;
                UniTask.Delay(TimeSpan.FromSeconds(timeout)).ContinueWith(() =>
                {
                    if (version == currentVersion)
                    {
                        Abort();
                    }
                }).Forget();
            }
        }

        private bool m_IsDisposed;
        /// <summary>
        /// Dispose is called only from the pool. You should not call it manually.
        /// </summary>
        public void Dispose()
        {
            if (m_IsDisposed)
            {
                return;
            }
            m_IsDisposed = true;
            #if DEBUG
            Instances.Remove(this);
            #endif
            Reset();
        }

        /// <summary>
        /// This method is called automatically by the pool when the instance is get from the pool.
        /// </summary>
        public void Initialize()
        {
            m_SignalSource = SignalSource.Create();
            m_AbortSignalSource = SignalSource.Create();
        }

        public event Action OnReset;

        /// <summary>
        /// This method is called automatically by the pool when the instance is returned to the pool.
        /// </summary>
        public void Reset()
        {
            unchecked
            {
                version++;
            }
            OnAbort?.Invoke();
            OnAbort = null;
            /* Call OnCancel event only if the cancellation is not requested yet. */
            if (state == AsyncCancellation.State.Idle)
            {
                OnCancel?.Invoke(false);
            }
            OnCancel = null;
            m_SignalSource.Dispose();
            m_AbortSignalSource.Dispose();
            state = AsyncCancellation.State.Idle;
            OnReset?.Invoke();
            OnReset = null;
        }

        public bool isAborted => state == AsyncCancellation.State.CancelledSync;
        public void Abort()
        {
            TryCancelSync();
        }

        public DisposableAction AddAbortedListenerOnce(Action action)
        {
            OnAbort += action;
            var currentVersion = this.version;
            return DisposableAction.Create(() =>
            {
                if (this.version == currentVersion)
                {
                    OnAbort -= action;
                }
            });
        }
    }
}