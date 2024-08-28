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
        public static readonly CancellationSignal Cancelled = new CancellationSignal();
        
        private readonly InternalCancellationSignalSource m_Source;
        public readonly int Version;
        // ReSharper disable once InconsistentNaming
        public bool IsCancellationRequested => GetSource()?.IsCancellationRequested ?? true;
        public CancellationSignal(InternalCancellationSignalSource source = null)
        {
            m_Source = source;
            Version = source?.Version ?? 0;
        }
        
        public DisposableAction Register(Action onCancelled)
        {
            if (m_Source != null && m_Source.Version == Version)
            {
                var memoizedSource = m_Source;
                void OnCancelled(int version)
                {
                    if (memoizedSource == null || memoizedSource.Version != version) return;
                    onCancelled?.Invoke();
                }
                return m_Source.Register(OnCancelled);
            }
            onCancelled?.Invoke();
            return DisposableAction.Empty;
        }
        
        public UniTask.Awaiter GetAwaiter()
        {
            return m_Source?.ToUniTask().GetAwaiter() ?? UniTask.CompletedTask.GetAwaiter();
        }
        
        public UniTask ToUniTask(bool cancelOnCancellationRequest = true)
        {
            return m_Source?.ToUniTask(cancelOnCancellationRequest) ?? UniTask.CompletedTask;
        }

        private InternalCancellationSignalSource GetSource()
        {
            if (m_Source != null && m_Source.Version == Version)
            {
                return m_Source;
            }
            return null;
        }
        
        /* Make it convertable to CancellationToken */
        public static implicit operator CancellationToken(CancellationSignal signal) => signal.GetSource()?.GetCancellationToken() ?? new CancellationToken(true);
    }

    public readonly struct CancellationSignalSource: IDisposable
    {
        public static CancellationSignalSource Create() => new CancellationSignalSource(InternalCancellationSignalSource.Pool.Get());
        
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
        
        private readonly InternalCancellationSignalSource m_InternalSource;
        private readonly int m_Version;
        
        public CancellationSignal Signal => new CancellationSignal(GetSource());
        
        public CancellationSignalSource(InternalCancellationSignalSource source)
        {
            m_InternalSource = source;
            m_Version = source.Version;
        }
        
        public void TryCancel()
        {
            #if DEBUG
            if (m_InternalSource == null)
            {
                Debug.LogError("CancellationSignalSource is null, but it should not be null. This is a bug");
                return;
            }
            #endif
            if (m_InternalSource.Version == m_Version)
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
            if (Pool.IsBelongsToPool(this))
            {
                Debug.LogError("CancellationSignalSource belongs to pool but was destroyed. It should be returned to the pool before destruction");
            }
            else
            {
                Debug.LogWarning("CancellationSignalSource was not disposed");
            }
        }
        
        internal void TryCancel()
        {
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
            IsCancellationRequested = false;
            Version++;
            Pool.Return(this);
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
        internal UniTask ToUniTask(bool cancelOnCancellationRequest = true)
        {
            if (IsCancellationRequested)
            {
                return cancelOnCancellationRequest ? UniTask.FromCanceled() : UniTask.CompletedTask;
            }
            var tcs = AutoResetUniTaskCompletionSource.Create();
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
            TryCancel();
            #if DEBUG
            if (Pool.IsBelongsToPool(this))
            {
                Debug.Log($"Can not dispose CancellationSignalSource because it belongs to pool. Return it to the pool instead");
            }
            #endif
            GC.SuppressFinalize(this);
        }
    }
}