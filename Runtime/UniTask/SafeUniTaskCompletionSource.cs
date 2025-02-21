using System;
using System.Diagnostics;
using System.Threading;
using Cysharp.Threading.Tasks;
using Debug = UnityEngine.Debug;

namespace Metamatic
{
    /// <summary>
    /// This struct is more safe than AutoResetUniTaskCompletionSource, because it can be used after it's returned to the pool. Access UniTask created by AutoResetUniTaskCompletionSource after it's returned to the pool will cause an unexpected behaviour.
    ///
    /// TODO: Replace all usages of AutoResetUniTaskCompletionSource with this struct.
    /// </summary>
    public readonly struct SafeUniTaskCompletionSource: IUniTaskSource
    {
        public static SafeUniTaskCompletionSource Create()
        {
            return new SafeUniTaskCompletionSource(AutoResetUniTaskCompletionSource.Create());
        }
        public static SafeUniTaskCompletionSource CreateFromCanceled(CancellationToken cancellationToken, out short token)
        {
            var source = Create();
            source.TrySetCanceled(cancellationToken);
            token = (short)source.version;
            return source;
        }
        
        public static SafeUniTaskCompletionSource CreateFromException(Exception exception, out short token)
        {
            var source = Create();
            source.TrySetException(exception);
            token = (short)source.version;
            return source;
        }
        
        public static SafeUniTaskCompletionSource CreateCompleted()
        {
            var source = Create();
            source.TrySetResult();
            return source;
        }
        
        private readonly AutoResetUniTaskCompletionSource m_OriginalCompletionSource;
        private readonly int m_Version;
        public int version => m_Version;
        // ReSharper disable once InconsistentNaming
        public UniTask Task => new UniTask(this, (short)m_Version);
        public bool isAlive => ResolveSource() != null;

        public UniTaskStatus GetStatus(short token)
        {
            return ResolveSource()?.GetStatus(token) ?? UniTaskStatus.Canceled;
        }

        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            var source = ResolveSource();
            if (source != null)
            {
                source.OnCompleted(continuation, state, token);
                return;
            }
            // TODO: Should we throw an exception here?
            Debug.LogWarning("[SafeTaskCompletionSource] Where are here. And what?"); // TODO: Remove this warning after testing.
        }

        public void GetResult(short token)
        {
            ResolveSource()?.GetResult(token);
        }

        public UniTaskStatus UnsafeGetStatus()
        {
            return ResolveSource()?.UnsafeGetStatus() ?? UniTaskStatus.Canceled;
        }
        
        public SafeUniTaskCompletionSource(AutoResetUniTaskCompletionSource completionSource)
        {
            m_OriginalCompletionSource = completionSource;
            m_Version = completionSource.version;
        }

        private AutoResetUniTaskCompletionSource ResolveSource()
        {
            if (m_OriginalCompletionSource != null && m_OriginalCompletionSource.version == m_Version)
            {
                return m_OriginalCompletionSource;
            }

            return null;
        }
        
        [DebuggerHidden]
        public bool TrySetResult()
        {
            var source = ResolveSource();
            if (source == null)
            {
                return false;
            }
            return source.TrySetResult();
        }
        
        [DebuggerHidden]
        public void TrySetResultSilently()
        {
            TrySetResult();
        }

        [DebuggerHidden]
        public bool TrySetCanceled(CancellationToken cancellationToken = default)
        {
            var source = ResolveSource();
            if (source == null)
            {
                Debug.LogError("Original completion source is not available.");
                return false;
            }
            return source.TrySetCanceled(cancellationToken);
        }
        
        [DebuggerHidden]
        public void TrySetCanceledSilently()
        {
            TrySetCanceled();
        }

        [DebuggerHidden]
        public bool TrySetException(Exception exception)
        {
            var source = ResolveSource();
            if (source == null)
            {
                Debug.LogError("Original completion source is not available.");
                return false;
            }
            return source.TrySetException(exception);
        }
    }
    
    /// <summary>
    /// This struct is more safe than AutoResetUniTaskCompletionSource, because it can be used after it's returned to the pool. Access UniTask created by AutoResetUniTaskCompletionSource after it's returned to the pool will cause an unexpected behaviour.
    ///
    /// TODO: Replace all usages of AutoResetUniTaskCompletionSource with this struct.
    /// </summary>
    public readonly struct SafeUniTaskCompletionSource<T>: IUniTaskSource<T>
    {
        public static SafeUniTaskCompletionSource<T> Create()
        {
            return new SafeUniTaskCompletionSource<T>(AutoResetUniTaskCompletionSource<T>.Create());
        }
        public static SafeUniTaskCompletionSource<T> CreateFromCanceled(CancellationToken cancellationToken, out short token)
        {
            var source = Create();
            source.TrySetCanceled(cancellationToken);
            token = (short)source.version;
            return source;
        }
        
        public static SafeUniTaskCompletionSource<T> CreateFromException(Exception exception, out short token)
        {
            var source = Create();
            source.TrySetException(exception);
            token = (short)source.version;
            return source;
        }
        
        public static SafeUniTaskCompletionSource<T> CreateCompleted(T value)
        {
            var source = Create();
            source.TrySetResult(value);
            return source;
        }
        
        private readonly AutoResetUniTaskCompletionSource<T> m_OriginalCompletionSource;
        private readonly int m_Version;
        public int version => m_Version;
        // ReSharper disable once InconsistentNaming
        public UniTask<T> Task => new UniTask<T>(this, (short)m_Version);
        public bool isAlive => ResolveSource() != null;

        public UniTaskStatus GetStatus(short token)
        {
            return ResolveSource()?.GetStatus(token) ?? UniTaskStatus.Canceled;
        }

        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            var source = ResolveSource();
            if (source != null)
            {
                source.OnCompleted(continuation, state, token);
                return;
            }
            // TODO: Should we throw an exception here?
            Debug.LogWarning("[SafeTaskCompletionSource] Where are here. And what?"); // TODO: Remove this warning after testing.
        }

        public void GetResult(short token)
        {
            ResolveSource().GetResult(token);
        }

        T IUniTaskSource<T>.GetResult(short token)
        {
            var source = ResolveSource();
            if (source == null)
            {
                return default;
            }
            return source.GetResult(token);
        }

        public UniTaskStatus UnsafeGetStatus()
        {
            return ResolveSource()?.UnsafeGetStatus() ?? UniTaskStatus.Canceled;
        }
        
        public SafeUniTaskCompletionSource(AutoResetUniTaskCompletionSource<T> completionSource)
        {
            m_OriginalCompletionSource = completionSource;
            m_Version = completionSource.version;
        }

        private AutoResetUniTaskCompletionSource<T> ResolveSource()
        {
            if (m_OriginalCompletionSource != null && m_OriginalCompletionSource.version == m_Version)
            {
                return m_OriginalCompletionSource;
            }

            return null;
        }
        
        [DebuggerHidden]
        public bool TrySetResult(T value)
        {
            var source = ResolveSource();
            if (source == null)
            {
                Debug.LogWarning("[SafeTaskCompletionSource] Original completion source is not available."); // TODO: Remove this warning after completing development and testing this struct, because it is normal scenario.
                return false;
            }
            return source.TrySetResult(value);
        }

        [DebuggerHidden]
        public bool TrySetCanceled(CancellationToken cancellationToken = default)
        {
            var source = ResolveSource();
            if (source == null)
            {
                Debug.LogError("Original completion source is not available.");
                return false;
            }
            return source.TrySetCanceled(cancellationToken);
        }

        [DebuggerHidden]
        public bool TrySetException(Exception exception)
        {
            var source = ResolveSource();
            if (source == null)
            {
                Debug.LogError("Original completion source is not available.");
                return false;
            }
            return source.TrySetException(exception);
        }
    }
}