using System;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    public readonly struct AbortableObserver : IDisposable
    {
        public static Func<IAsyncDisposableAbortable, AbortableObserverProxy> CreateProxy(IAbortable abortable)
        {
            return targetDisposable => new AbortableObserverProxy(abortable, targetDisposable);
        }
        
        public static Func<IAsyncDisposableAbortable, AbortableObserverProxy> CreateProxy(params IAbortable[] abortable)
        {
            return targetDisposable => new AbortableObserverProxy(abortable, targetDisposable);
        }
        
        public static AbortableObserver Observe(IAbortable abortable, IDisposable targetDisposable)
        {
            return new AbortableObserver(abortable, targetDisposable);
        }
        
        public static AbortableObserver Observe(IAbortable abortable, Action abort)
        {
            return new AbortableObserver(abortable, abort);
        }
        
        private readonly Action m_ReleaseFinalizer;
        
        public AbortableObserver(IAbortable abortable, IDisposable targetDisposable)
        {
            if (abortable.isAborted)
            {
                targetDisposable.Dispose();
                m_ReleaseFinalizer = null;
            }
            else
            {
                m_ReleaseFinalizer = abortable.AddAbortedListenerOnce(targetDisposable.Dispose);
            }
        }
        
        public AbortableObserver(IAbortable abortable, Action abort)
        {
            if (abortable.isAborted)
            {
                abort();
                m_ReleaseFinalizer = null;
            }
            else
            {
                m_ReleaseFinalizer = abortable.AddAbortedListenerOnce(abort);
            }
        }

        public void Dispose()
        {
            m_ReleaseFinalizer?.Invoke();
        }
    }
    /// <summary>
    /// Allows to observe abortable objects and dispose target disposable when the abortable object is aborted. Main goal is to be able to abort async disposable operations when the abortable object is aborted.
    /// It works as a proxy, calling Dispose and DisposeAsync on the target disposable when same methods are called on the AbortableObserver.
    /// </summary>
    /// <example>
    /// var withAbortable = AbortableObserver.CreateWrapper(abortable1, abortable2, abortable3);
    /// await using var operation = withAbortable(AsyncOperation());
    /// </example>
    public readonly struct AbortableObserverProxy: IAsyncDisposableAbortable
    {
        private readonly IAsyncDisposableAbortable m_TargetDisposable;
        private readonly Action m_ReleaseFinalizer;
        
        public AbortableObserverProxy(IAbortable abortable, IAsyncDisposableAbortable targetDisposable)
        {
            m_TargetDisposable = targetDisposable;

            if (abortable.isAborted)
            {
                targetDisposable.Abort();
                m_ReleaseFinalizer = null;
            }
            else
            {
                m_ReleaseFinalizer = abortable.AddAbortedListenerOnce(targetDisposable.Abort);
            }
        }
        
        public AbortableObserverProxy(IAbortable[] abortables, IAsyncDisposableAbortable targetDisposable)
        {
            m_TargetDisposable = targetDisposable;

            foreach (var abortable in abortables)
            {
                if (abortable.isAborted)
                {
                    targetDisposable.Abort();
                    m_ReleaseFinalizer = null;
                    return;
                }
            }

            Action onRelease = null;
            
            foreach (var abortable in abortables)
            {
                onRelease += abortable.AddAbortedListenerOnce(targetDisposable.Abort);
            }

            m_ReleaseFinalizer = onRelease;
        }

        public void Dispose()
        {
            m_ReleaseFinalizer?.Invoke();
            m_TargetDisposable.Abort();
        }

        public async UniTask DisposeAsync()
        {
            try
            {
                await m_TargetDisposable.DisposeAsync();
            }
            finally
            {
                m_ReleaseFinalizer?.Invoke();
            }
        }

        public bool isAborted => m_TargetDisposable.isAborted;
        public void Abort()
        {
            m_TargetDisposable.Abort();
        }

        public DisposableAction AddAbortedListenerOnce(Action action)
        {
            return m_TargetDisposable.AddAbortedListenerOnce(action);
        }

        public bool isDisposedAsync => m_TargetDisposable.isDisposedAsync;
    }
}