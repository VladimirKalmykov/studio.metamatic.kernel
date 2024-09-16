using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Cysharp.Threading.Tasks;
using Metamatic.DevTools;
using UnityEngine;
using UnityEngine.Serialization;

namespace Metamatic
{
    /// <summary>
    /// CancellationToken like signal for cancellation optimized for Unity.
    /// Primary difference is that CancellationToken is guaranteed to be cancelled. Cancelled token loose the connection to the source, thus the source returns to the pool and can be immediately reused.
    /// CancellationSignal can be easily converted to CancellationToken or UniTask. It can be awaited by GetAwaiter() or ToUniTask() multiple times. It uses UniTask pool, thus it is very lightweight.
    /// Try to avoid converting CancellationSignal to CancellationToken, because CancellationSignal is more optimized for Unity.
    /// </summary>
    public readonly struct CancellationSignal
    {
        public static readonly CancellationSignal Cancelled = new (true);
        public static readonly CancellationSignal None = new (false);
        
        private readonly InternalCancellationSignalSource m_Source;
        private readonly CancellationToken m_CancellationToken;
        // ReSharper disable once MemberCanBePrivate.Global
        public readonly int Version;
        // ReSharper disable once InconsistentNaming
        public bool IsCancellationRequested
        {
            get {
                if (m_Source == null)
                {
                    /* If version is 0, it is never cancelled but only if source equals null */
                    return false;
                }
                /* If version is -1, it is always cancelled */
                if (Version == -1 || m_Source.Version != Version)
                {
                    return true;
                }
                /* Otherwise, check the source or return true (version is equal to zero or greater plus source version is not equal to current version — signal is cancelled) */
                return m_Source.IsCancellationRequested;
            }
        }
        // ReSharper disable once InconsistentNaming
        public bool CanBeCanceled => m_Source != null;
        public CancellationSignal(InternalCancellationSignalSource source = null)
        {
            m_Source = source;
            Version = source?.Version ?? 0;
        }
        
        public CancellationSignal(bool cancelled)
        {
            m_Source = null;
            Version = cancelled ? -1 : 0;
        }
        
        public CancellationSignal(CancellationToken token)
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
        
        public UniTask ToUniTask(bool cancelOnCancellationRequest = false)
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
            
            return GetSource()?.ToUniTask(cancelOnCancellationRequest) ?? UniTask.CompletedTask;
        }
        
        public void ThrowIfCancellationRequested()
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
        public static implicit operator CancellationToken(CancellationSignal signal)
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

    public readonly struct CancellationSignalSource: IDisposable
    {
        #if DEBUG
        public static Dictionary<string, InternalCancellationSignalSource> DebugSources = new();
        public static InternalCancellationSignalSource GetFromPool()
        {
            var icss = InternalCancellationSignalSource.Pool.Get();
            return icss;
        }
        #endif
        public static CancellationSignalSource Create(
            #if DEBUG
            string debugId = null
            #endif
            )
        {   
            
            var icss = GetFromPool();
#if DEBUG
            icss.DebugCreateStackTrace = Environment.StackTrace;
            icss.DebugId = debugId ?? Guid.NewGuid().ToString();
            DebugSources[icss.DebugId] = icss;
            icss.DebugOnCancel += () =>
            {
                DebugSources.Remove(icss.DebugId);
            };
#endif
            return new CancellationSignalSource(icss);
        }

        public static CancellationSignalSource CreateLinked(params CancellationSignal[] signals)
        {
            /* First, check if all signals are not cancelled */
            foreach (var signal in signals)
            {
                if (signal.IsCancellationRequested)
                {
                    return new CancellationSignalSource();
                }
            }
            var icss = GetFromPool();
            
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
            
            
            var memoizedVersion = icss.Version;
            void Cancel()
            {
                icss.TryCancel(memoizedVersion);
            }
            var css = new CancellationSignalSource(icss);
            
            
            foreach (var signal in signals)
            {
                signal.Register(Cancel);
            }

            return css;
        }

        public static CancellationSignalSource CreateLinked(params CancellationToken[] tokens)
        {
            /* First, check if all tokens are not cancelled */
            foreach (var token in tokens)
            {
                if (token.IsCancellationRequested)
                {
                    return new CancellationSignalSource();
                }
            }
            var icss = GetFromPool();
            
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
            
            var memoizedVersion = icss.Version;
            void Cancel()
            {
                icss.TryCancel(memoizedVersion);
            }
            
            var css = new CancellationSignalSource(icss);
            
            foreach (var token in tokens)
            {
                token.Register(Cancel);
            }

            return css;
        }
        
        private readonly InternalCancellationSignalSource m_InternalSource;
        private readonly int m_Version;

        /// <summary>
        /// TODO: It is incorrect to return new CancellationSignal every time. It should return the same signal
        /// </summary>
        // ReSharper disable once InconsistentNaming
        public CancellationSignal Signal
        {
            get
            {
                var source = GetSource();
                if (source == null)
                {
                    return CancellationSignal.Cancelled;
                }
                return new CancellationSignal(source);
            }
        }
        // ReSharper disable once InconsistentNaming
        public bool IsCancellationRequested => GetSource()?.IsCancellationRequested ?? true;
        // ReSharper disable once InconsistentNaming
        public bool CanBeCancelled => GetSource() != null;
        
        internal CancellationSignalSource(InternalCancellationSignalSource source)
        {
            Console.Verbose("CancellationSignal", "Create", $"Id: {source.DebugId} Version: {source.Version}");
            m_InternalSource = source;
            m_Version = source.Version;
        }
        
        public void TryCancel()
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
            TryCancel();
        }
    }
    
    public class InternalCancellationSignalSource: IDisposable
    {
        internal static readonly ParamLessPool<InternalCancellationSignalSource> Pool = new(autoShrink: true, capacity: 100);

        #if DEBUG
        static InternalCancellationSignalSource()
        {
            Monitoring.Create("CancellationSignal", "Sources count out of pool", () => Pool.OutOfPoolCount);
            Monitoring.Create("CancellationSignal", "Sources count in pool", () => Pool.Count);
            var commander = new Commander();
            commander.RegisterCommand("List", s =>
            {
                Console.Log("CancellationSignal", "Total count", CancellationSignalSource.DebugSources.Count.ToString(), Console.LogLevel.Interactive, Pool.Count.ToString());
                foreach (var source in CancellationSignalSource.DebugSources)
                {
                    var createdAgo = Time.realtimeSinceStartup - source.Value.DebugCreateAt;
                    Console.Log("CancellationSignal", "Source", $"{source.Key} {createdAgo}s", Console.LogLevel.Interactive, source.Value.DebugCreateStackTrace);
                }
            });
            Console.RegisterCommand("CancellationSignal", commander);
        }

        public string DebugCreateStackTrace = "";
        public float DebugCreateAt = Time.realtimeSinceStartup;
        public string DebugId = null;
        public event Action DebugOnCancel;
        #endif
        
        // ReSharper disable once InconsistentNaming
        public bool IsCancellationRequested { get; private set; }
        // ReSharper disable once InconsistentNaming
        public int Version { get; private set; }
        // ReSharper disable once InconsistentNaming
        public CancellationSignal Signal => new CancellationSignal(this);
        private CancellationTokenSource m_CancellationTokenSource;
        private event Action<int> m_OnCancelled;
        
        ~InternalCancellationSignalSource()
        {
            FinalizeFromMainThread().Forget();
        }
        
        private async UniTaskVoid FinalizeFromMainThread()
        {
            #if DEBUG
            if (DebugId != null)
            {
                Console.Verbose("CancellationSignalSource", $"Finalize {DebugId}");
            }
            #endif
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
#if DEBUG
            if (DebugId != null)
            {
                Console.Verbose("CancellationSignal", "TryCancel", $"Try Cancel {DebugId} Version: {versionToCancel} (current version: {Version})");
            }
#endif
            if (IsCancellationRequested || versionToCancel != Version) return;
#if DEBUG
            Console.Verbose("CancellationSignal", "Return", $"Id: {DebugId}, Version: {Version}");
#endif
            var cancelledVersion = Version;
            unchecked
            {
                Version++;
            }
           
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
                Pool.Return(this);
            }
            #if DEBUG
            DebugOnCancel?.Invoke();
            DebugOnCancel = null;
            #endif
        }
        

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
            var tcs = SafeTaskCompletionSource.Create();
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

        public void Dispose()
        {
#if DEBUG
            if (DebugId != null)
            {
                Debug.Log($"[CancellationSignalSource] Dispose {DebugId}");
            }
#endif
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