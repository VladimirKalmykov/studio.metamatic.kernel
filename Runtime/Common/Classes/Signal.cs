using System;
using System.Diagnostics;
using System.Threading;
using Cysharp.Threading.Tasks;
using Metamatic.DevTools;
using UnityEngine;
using UnityEngine.Serialization;
using Debug = UnityEngine.Debug;

namespace Metamatic
{
    /// <summary>
    /// CancellationToken like signal for cancellation optimized for Unity.
    /// Primary difference is that CancellationToken is guaranteed to be cancelled. Cancelled token loose the connection to the source, thus the source returns to the pool and can be immediately reused.
    /// CancellationSignal can be easily converted to CancellationToken or UniTask. It can be awaited by GetAwaiter() or ToUniTask() multiple times. It uses UniTask pool, thus it is very lightweight.
    /// Try to avoid converting CancellationSignal to CancellationToken, because CancellationSignal is more optimized for Unity.
    /// </summary>
    /// <remarks>
    /// // TODO: Consider to rename to Signal and SignalSource instead of CancellationSignal and CancellationSignalSource
    /// </remarks>
    public readonly struct Signal
    {
        /// <summary>
        /// Returns already raised signal
        /// </summary>
        public static readonly Signal Raised = new (true);
        /// <summary>
        /// Return signal that is never cancelled
        /// </summary>
        public static readonly Signal None = new (false);
        
        private readonly InternalCancellationSignalSource m_Source;
        private readonly CancellationToken m_CancellationToken;
        // ReSharper disable once MemberCanBePrivate.Global
        public readonly int Version;
        /// <summary>
        /// Backward compatibility with CancellationToken interface (use can use it in a usual way)
        /// </summary>
        // ReSharper disable once InconsistentNaming
        public bool IsCancellationRequested => isRaised;
        
        /// <summary>
        /// Determines if the signal is triggered. It is the same as IsCancellationRequested, but it is more readable and semantically correct for Signal.
        /// </summary>
        public bool isRaised
        {
            get {
                if (Version == -2) return m_CancellationToken.IsCancellationRequested;
                if (m_Source == null)
                {
                    /* If version is 0, it is never cancelled but only if source equals null */
                    return Version == -1;
                }
                /* If version is -1, it is always cancelled */
                return m_Source.Version != Version || m_Source.IsCancellationRequested;
            }
        }
        // ReSharper disable once InconsistentNaming
        public bool CanBeCanceled => m_Source != null;
        public Signal(InternalCancellationSignalSource source = null)
        {
            m_Source = source;
            Version = source?.Version ?? 0;
        }
        
        public Signal(bool cancelled)
        {
            m_Source = null;
            Version = cancelled ? -1 : 0;
        }
        
        public Signal(CancellationToken token)
        {
            m_Source = null;
            m_CancellationToken = token;
            Version = -2;
        }
        
        public DisposableAction Register(Action onCancelled)
        {
            if (Version == -1)
            {
                /* Forever cancelled */
                onCancelled?.Invoke();
                return DisposableAction.Empty;
            }
            
            if (Version == -2)
            {
                /* Register to CancellationToken */
                if (m_CancellationToken.IsCancellationRequested)
                {
                    onCancelled?.Invoke();
                    return DisposableAction.Empty;
                }
                void OnCancelled()
                {
                    onCancelled?.Invoke();
                }
                var registration = m_CancellationToken.Register(OnCancelled);
                return new DisposableAction(() =>
                {
                    registration.Dispose();
                });
            }

            if (m_Source == null)
            {
                return DisposableAction.Empty;
            }
            
            if (m_Source.Version == Version)
            {
                var memoizedSource = m_Source;
                var memoizedVersion = m_Source.Version;
                void OnCancelled(int version)
                {
                    if (memoizedSource == null || memoizedVersion != version) return;
                    onCancelled?.Invoke();
                }
                return m_Source.Register(OnCancelled);
            }
            return DisposableAction.Empty;
        }
        
        public UniTask.Awaiter GetAwaiter()
        {
            if (Version == -1)
            {
                /* Fixed value, to return completed task */
                return UniTask.CompletedTask.GetAwaiter();
            }
            if (Version == -2)
            {
                /* Register to CancellationToken */
                if (!m_CancellationToken.CanBeCanceled || m_CancellationToken.IsCancellationRequested)
                {
                    return UniTask.CompletedTask.GetAwaiter();
                }
                
                return m_CancellationToken.ToCompletionUniTask().GetAwaiter();
            }
            
            /* Signal without source is always cancelled because fixed non-cancelled signal will be awaited forever */
            if (m_Source == null)
            {
                return UniTask.CompletedTask.GetAwaiter();
            }
            
            return GetSource()?.ToUniTask().GetAwaiter() ?? UniTask.CompletedTask.GetAwaiter();
        }
        
        /// <summary>
        /// Convert signal to UniTask. If cancelOnRaised is true, the task will be cancelled when the signal is raised.
        /// </summary>
        /// <param name="cancelOnRaised"></param>
        /// <returns></returns>
        public UniTask ToUniTask(bool cancelOnRaised = false)
        {
            if (Version == -1)
            {
                /* Fixed value, to return completed task */
                return UniTask.CompletedTask;
            }
            if (Version == -2)
            {
                /* Register to CancellationToken */
                if (!m_CancellationToken.CanBeCanceled || m_CancellationToken.IsCancellationRequested)
                {
                    return UniTask.CompletedTask;
                }
                
                return m_CancellationToken.ToCompletionUniTask();
            }
            
            /* Signal without source is always cancelled because fixed non-cancelled signal will be awaited forever */
            if (m_Source == null)
            {
                return UniTask.CompletedTask;
            }
            
            return GetSource()?.ToUniTask(cancelOnRaised) ?? UniTask.CompletedTask;
        }
        
        public void ThrowIfRaised()
        {
            if (IsCancellationRequested)
            {
                throw new OperationCanceledException();
            }
        }

        private InternalCancellationSignalSource GetSource()
        {
            if (Version >= 0 && m_Source != null && m_Source.Version == Version)
            {
                return m_Source;
            }
            return null;
        }
        
        /* Make it convertable to CancellationToken */
        public static implicit operator CancellationToken(Signal signal)
        {
            if (signal.Version == -2)
            {
                return signal.m_CancellationToken;
            }
            
            if (signal.Version == -1)
            {
                return new CancellationToken(true);
            }
            
            if (signal.m_Source == null)
            {
                return CancellationToken.None;
            }

            return signal.GetSource()?.GetCancellationToken() ?? CancellationToken.None;
        }
    }

    public readonly struct SignalSource: IDisposable
    {
        public static readonly SignalSource Empty = new SignalSource();
        public static InternalCancellationSignalSource GetFromPool()
        {
            var icss = InternalCancellationSignalSource.Get();
            return icss;
        }
        
        public static SignalSource Create(
            #if DEBUG
            string debugId = null
            #endif
            )
        {   
            
            var icss = GetFromPool();
#if DEBUG
            var stackFrame = new System.Diagnostics.StackFrame(1, true);
            icss.DebugCreateStackFrame = stackFrame;
            icss.DebugCreateAt = Time.realtimeSinceStartup;
#endif
            return new SignalSource(icss);
        }

        public static SignalSource CreateLinked(params Signal[] signals)
        {
            /* First, check if all signals are not cancelled */
            var someCanBeCancelled = false;
            foreach (var signal in signals)
            {
                if (signal.isRaised)
                {
                    return new SignalSource();
                } else if (signal.CanBeCanceled)
                {
                    someCanBeCancelled = true;
                }
            }

            if (!someCanBeCancelled)
            {
                /* If all signals are not cancellable, just return new signal source without linking */
                return Create();
            }
            var icss = GetFromPool();
            
            /*
            #if DEBUG
            icss.DebugCreateStackTrace = Environment.StackTrace;
            if (icss.DebugId == null)
            {
                icss.DebugId = "Linked_" + Guid.NewGuid().ToString();
            }
            DebugSources[icss.DebugId] = icss;
            icss.DebugOnCancel += () =>
            {
                DebugSources.Remove(icss.DebugId);
            };
            #endif
            */
            
            
            var memoizedVersion = icss.Version;
            Action actionOnCancel = null;
            void Cancel()
            {
                icss.TryCancel(memoizedVersion);
            }
            var css = new SignalSource(icss);
            
            foreach (var signal in signals)
            {
                actionOnCancel += signal.Register(Cancel).Dispose;
            }
            
            actionOnCancel += icss.Register((version) =>
            {
                actionOnCancel?.Invoke();
            });

            return css;
        }

        public static SignalSource CreateLinked(params CancellationToken[] tokens)
        {
            /* First, check if all tokens are not cancelled */
            var someCanBeCancelled = false;
            foreach (var token in tokens)
            {
                if (token.IsCancellationRequested)
                {
                    return new SignalSource();
                } else if (token.CanBeCanceled)
                {
                    someCanBeCancelled = true;
                }
            }
            if (!someCanBeCancelled)
            {
                /* If all signals are not cancellable, just return new signal source without linking */
                return Create();
            }
            var icss = GetFromPool();
       
/*            
#if DEBUG
            icss.DebugCreateStackTrace = Environment.StackTrace;
            if (icss.DebugId == null)
            {
                icss.DebugId = $"LinkedToTokens_{Guid.NewGuid()}";
            }
            
            DebugSources[icss.DebugId] = icss;
            icss.DebugOnCancel += () =>
            {
                DebugSources.Remove(icss.DebugId);
            };
#endif
*/
            
            Action actionOnCancel = null;
            var memoizedVersion = icss.Version;
            void Cancel()
            {
                icss.TryCancel(memoizedVersion);
            }
            
            var css = new SignalSource(icss);
            
            foreach (var token in tokens)
            {
                actionOnCancel += token.Register(Cancel).Dispose;
            }
            
            actionOnCancel += icss.Register((version) =>
            {
                actionOnCancel?.Invoke();
            });

            return css;
        }
        
        private readonly InternalCancellationSignalSource m_InternalSource;
        private readonly int m_Version;

        /// <summary>
        /// TODO: It is incorrect to return new CancellationSignal every time. It should return the same signal
        /// </summary>
        // ReSharper disable once InconsistentNaming
        public Signal Signal
        {
            get
            {
                var source = GetSource();
                if (source == null)
                {
                    return Signal.Raised;
                }
                return new Signal(source);
            }
        }
        // ReSharper disable once InconsistentNaming
        public bool IsCancellationRequested => IsRaised;
        // ReSharper disable once InconsistentNaming
        public bool IsRaised => GetSource()?.IsCancellationRequested ?? true;
        // ReSharper disable once InconsistentNaming
        public bool CanBeCancelled => GetSource() != null;
        public bool isAlive => GetSource() != null;
        
        internal SignalSource(InternalCancellationSignalSource source)
        {
            m_InternalSource = source;
            m_Version = source.Version;
        }
        
        public void TryRaise()
        {
            /* m_InternalSource can be null, because this struct can be created by default */
            if (m_InternalSource != null)
            {
                m_InternalSource.TryCancel(m_Version);
            }
        }
        
        private InternalCancellationSignalSource GetSource()
        {
            if (m_InternalSource != null && m_InternalSource.Version == m_Version)
            {
                return m_InternalSource;
            }
            return null;
        }
        
        /// <summary>
        /// Dispose the source and cancel all the signals. You do not need to call this method if you already called TryCancel, but it is safe to call it multiple times.
        /// </summary>
        public void Dispose()
        {
            TryRaise();
        }
    }
    
    public class InternalCancellationSignalSource: IDisposable
    {
        private static readonly ParamLessPool<InternalCancellationSignalSource> Pool = new(autoShrink: true, capacity: 10000);

        public static InternalCancellationSignalSource Get()
        {
            return Pool.Get();
        }
        
        public static void Return(InternalCancellationSignalSource source)
        {
            Pool.Return(source);
        }
        #if DEBUG
        static InternalCancellationSignalSource()
        {
            Monitoring.Create("CancellationSignal", "Sources count out of pool", () => Pool.OutOfPoolCount);
            Monitoring.Create("CancellationSignal", "Sources count in pool", () => Pool.InPoolCount);
            /*
            var commander = new Commander();
            commander.RegisterCommand("List", s =>
            {
                foreach (var source in SignalSource.DebugSources)
                {
                    var createdAgo = Time.realtimeSinceStartup - source.Value.DebugCreateAt;
                }
            });
            
            Console.RegisterCommand("CancellationSignal", commander);
            */
        }

        public StackFrame DebugCreateStackFrame;
        public float DebugCreateAt;
        public readonly string DebugId;
        [FormerlySerializedAs("LastVersionChangeAt")] public float DebugLastVersionChangeAt;

        /*
        public string DebugCreateStackTrace = "";
        public float DebugCreateAt = Time.realtimeSinceStartup;
        public string DebugId = null;
        public event Action DebugOnCancel;
        */
        #endif
        
        // ReSharper disable once InconsistentNaming
        public bool IsCancellationRequested { get; private set; }
        // ReSharper disable once InconsistentNaming
        public int Version { get; private set; }
        // ReSharper disable once InconsistentNaming
        public Signal Signal => new Signal(this);
        private CancellationTokenSource m_CancellationTokenSource;
        private event Action<int> m_OnCancelled;
        
        #if DEBUG
        public InternalCancellationSignalSource()
        {
            DebugLastVersionChangeAt = Time.realtimeSinceStartup;
            DebugId = Guid.NewGuid().ToString();
            // DebugStartStagnationCheck();
        }
        #endif
        
        ~InternalCancellationSignalSource()
        {
            FinalizeFromMainThread().Forget();
        }
        
        private async UniTaskVoid FinalizeFromMainThread()
        {
            /*
            #if DEBUG
            if (DebugId != null)
            {
                Console.Verbose("CancellationSignalSource", $"Finalize {DebugId}");
            }
            #endif
            */
            await UniTask.SwitchToMainThread();
            if (Pool.IsBelongsToPool(this))
            {
                Pool.HardDetach(this);
            }
            else
            {
                Console.Warning("CancellationSignalSource", "Was not disposed properly");
                Dispose();
            }
        }

        /// <summary>
        /// Try cancel signal
        /// </summary>
        /// <param name="versionToCancel">Version to cancel</param>
        /// <param name="isFinal">Is called from finalizer or dispose</param>
        internal void TryCancel(int versionToCancel, bool isFinal = false)
        {
            /*
#if DEBUG
            if (DebugId != null)
            {
                Console.Verbose("CancellationSignal", "TryCancel", $"Try Cancel {DebugId} Version: {versionToCancel} (current version: {Version})");
            }
#endif
*/          
            if (IsCancellationRequested || versionToCancel != Version) return;
            var cancelledVersion = Version;
            unchecked
            {
                Version++;
            }
            
            #if DEBUG
            DebugLastVersionChangeAt = Time.realtimeSinceStartup;
            // DebugStartStagnationCheck();
            #endif
           
            IsCancellationRequested = true;
            
            if (m_OnCancelled != null)
            {
                m_OnCancelled.Invoke(cancelledVersion);
                m_OnCancelled = null;
            }

            if (m_CancellationTokenSource != null)
            {
                if (!m_CancellationTokenSource.IsCancellationRequested)
                {
                    m_CancellationTokenSource.Cancel();
                }
                m_CancellationTokenSource.Dispose();
                m_CancellationTokenSource = null;
            }
            
            if (!isFinal)
            {
                IsCancellationRequested = false;
                Return(this);
            }
            #if DEBUG
            /*
            DebugOnCancel?.Invoke();
            DebugOnCancel = null;
            */
            #endif
        }
        
        #if DEBUG
        /*
        private bool DebugCheckStagnation()
        {
            if (Time.realtimeSinceStartup - DebugLastVersionChangeAt > 290f)
            {
                Console.Debug("SignalSource", "Stagnation", $"{DebugId} is stagnated. Stack trace: {DebugCreateStackFrame}");
                return true;
            }

            return false;
        }
        
        private void DebugStartStagnationCheck()
        {
            UniTask.Delay(TimeSpan.FromSeconds(300), ignoreTimeScale: true).ContinueWith(() =>
            {
                if (m_IsDisposed || Pool.IsInPool(this)) return;
                if (!DebugCheckStagnation())
                {
                    DebugStartStagnationCheck();
                }
                
            }).Forget();
        }
        */
        #endif
            
        internal CancellationToken GetCancellationToken()
        {
            if (m_CancellationTokenSource == null)
            {
                m_CancellationTokenSource = new CancellationTokenSource();
            }
            return m_CancellationTokenSource.Token;
        }
        
        /// <summary>
        /// Create a UniTask that completes or cancels when cancellation requested.
        /// </summary>
        /// <param name="cancelOnCancellationRequest"></param>
        /// <returns></returns>
        internal UniTask ToUniTask(bool cancelOnCancellationRequest = false)
        {
            if (IsCancellationRequested)
            {
                return cancelOnCancellationRequest ? UniTask.FromCanceled() : UniTask.CompletedTask;
            }
            var tcs = SafeUniTaskCompletionSource.Create();
            var memoizedVersion = Version;
            m_OnCancelled += (version) =>
            {
                if (version != memoizedVersion) return;
                if (cancelOnCancellationRequest)
                {
                    tcs.TrySetCanceled();
                }
                else
                {
                    tcs.TrySetResult();
                }
            };
            return tcs.Task;
        }
        
        internal DisposableAction Register(Action<int> onCancelled)
        {
            if (IsCancellationRequested)
            {
                onCancelled?.Invoke(Version);
                return DisposableAction.Empty;
            }
            m_OnCancelled += onCancelled;
            var version = Version;
            return new DisposableAction(() =>
            {
                if (Version != version) return;
                m_OnCancelled -= onCancelled;
            });
        }

        private bool m_IsDisposed;
        public void Dispose()
        {
            if (m_IsDisposed) return;
            m_IsDisposed = true;
            TryCancel(Version, true);
            #if DEBUG
            if (Pool.IsBelongsToPool(this))
            {
                Debug.LogError($"Can not dispose CancellationSignalSource because it belongs to pool. Return it to the pool instead");
            }
            #endif
            GC.SuppressFinalize(this);
        }
    }
}