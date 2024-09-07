using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

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
                /* If version is -1, it is always cancelled */
                if (Version == -1)
                {
                    return true;
                }
                if (m_Source == null)
                {
                    /* If version is 0, it is never cancelled but only if source equals null */
                    return false;
                }
                /* Otherwise, check the source or return true (version is equal to zero or greater plus source version is not equal to current version — signal is cancelled) */
                return GetSource()?.IsCancellationRequested ?? true;
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
                void OnCancelled(int version)
                {
                    if (memoizedSource == null || memoizedSource.Version != version) return;
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
        public static CancellationSignalSource Create(
            #if DEBUG
            string debugId = null
            #endif
            )
        {
            var icss = InternalCancellationSignalSource.Pool.Get();
            #if DEBUG
            if (debugId != null)
            {
                icss.debugId = debugId;
            }
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
            var source = InternalCancellationSignalSource.Pool.Get();
            var memoizedVersion = source.Version;
            void Cancel()
            {
                if (memoizedVersion != source.Version) return;
                source.TryCancel();
            }
            foreach (var signal in signals)
            {
                signal.Register(Cancel);
            }
            return new CancellationSignalSource(source);
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
            var source = InternalCancellationSignalSource.Pool.Get();
            var memoizedVersion = source.Version;
            void Cancel()
            {
                if (memoizedVersion != source.Version) return;
                source.TryCancel();
            }
            foreach (var token in tokens)
            {
                token.Register(Cancel);
            }
            return new CancellationSignalSource(source);
        }
        
        private readonly InternalCancellationSignalSource m_InternalSource;
        private readonly int m_Version;
        
        // ReSharper disable once InconsistentNaming
        public CancellationSignal Signal => new CancellationSignal(GetSource());
        // ReSharper disable once InconsistentNaming
        public bool IsCancellationRequested => GetSource()?.IsCancellationRequested ?? true;
        // ReSharper disable once InconsistentNaming
        public bool CanBeCancelled => m_InternalSource != null;
        
        internal CancellationSignalSource(InternalCancellationSignalSource source)
        {
            m_InternalSource = source;
            m_Version = source.Version;
        }
        
        public void TryCancel()
        {
            /* m_InternalSource can be null, because this struct can be created by default */
            if (m_InternalSource != null && m_InternalSource.Version == m_Version)
            {
                m_InternalSource.TryCancel();
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
        internal static readonly ParamLessPool<InternalCancellationSignalSource> Pool = new(autoShrink: true, capacity: 10);
        
        #if DEBUG
        public string debugId = null;
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
            if (debugId != null)
            {
                Console.Verbose("CancellationSignalSource", $"Finalize {debugId}");
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
        /// <param name="isFinal">Is called from finalizer or dispose</param>
        internal void TryCancel(bool isFinal = false)
        {
#if DEBUG
            if (debugId != null)
            {
                Debug.Log($"[CancellationSignalSource] TryCancel {debugId}");
            }
#endif
            if (IsCancellationRequested) return;
            IsCancellationRequested = true;
            
            if (m_OnCancelled != null)
            {
                m_OnCancelled.Invoke(Version);
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

            unchecked
            {
                Version++;
            }
            IsCancellationRequested = false;
            if (!isFinal)
            {
                Pool.Return(this);
            }
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
            m_OnCancelled += (version) =>
            {
                if (version != Version) return; // TODO: Do I need to check version?
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
            if (debugId != null)
            {
                Debug.Log($"[CancellationSignalSource] Dispose {debugId}");
            }
#endif
            TryCancel(true);
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