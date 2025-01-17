using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic
{
    public abstract class SequentialHandlerBase: IDisposable
    {
        protected Signal externCancellationToken;
        protected SignalSource internalCancellationTokenSource;
        public bool disposed { get; private set; }
        protected void ResetInternalCancellationSignal()
        {
            internalCancellationTokenSource.TryRaise();
            internalCancellationTokenSource = SignalSource.CreateLinked(externCancellationToken);
        }
        public void Dispose()
        {
            disposed = true;
            internalCancellationTokenSource.TryRaise();
            EraseProps();
        }

        protected abstract void EraseProps();
    }
    public class SequentialHandler: SequentialHandlerBase
    {
        public static DisposableAction Subscribe(Func<Action, IDisposable> subscribe, Handler handler)
        {
            var handlerDisposable = new SequentialHandler(handler);
            var subscriptionDisposable = subscribe(handlerDisposable.Handle);
            return new DisposableAction(() =>
            {
                handlerDisposable.Dispose();
                subscriptionDisposable.Dispose();
            });
        }
        public delegate void Handler(Signal signal);

        private Handler m_Handler;
    
        public SequentialHandler(Handler handler, Signal externalCancellationToken = default)
        {
            m_Handler = handler;
            externCancellationToken = externalCancellationToken;
        }
        public void Handle()
        {
            Debug.Assert(!disposed, "SequentialHandler disposed but still used");
            Debug.Assert(m_Handler != null, "Handler is null");
            ResetInternalCancellationSignal();
            m_Handler(internalCancellationTokenSource.Signal);
        }

        protected override void EraseProps()
        {
            m_Handler = default;
        }
    }
    public class SequentialHandler<T>: SequentialHandlerBase
    {
        public static DisposableAction Subscribe(Func<Action<T>, DisposableAction> subscribe, Handler handler)
        {
            var handlerDisposable = new SequentialHandler<T>(handler);
            var subscriptionDisposable = subscribe(handlerDisposable.Handle);
            return new DisposableAction(() =>
            {
                handlerDisposable.Dispose();
                subscriptionDisposable.Dispose();
            });
        }
        public static DisposableAction Subscribe(Func<Action<T>, DisposableAction> subscribe, Func<T, Signal, UniTaskVoid> asyncHandler)
        {
            var handlerDisposable = new SequentialHandler<T>(asyncHandler);
            var subscriptionDisposable = subscribe(handlerDisposable.Handle);
            return new DisposableAction(() =>
            {
                handlerDisposable.Dispose();
                subscriptionDisposable.Dispose();
            });
        }
        public static DisposableAction WatchAsync(Func<Action<T>, DisposableAction> subscribe, Func<T, Signal, UniTaskVoid> asyncHandler)
        {
            var handlerDisposable = new SequentialHandler<T>(asyncHandler);
            var subscriptionDisposable = subscribe(handlerDisposable.Handle);
            return new DisposableAction(() =>
            {
                handlerDisposable.Dispose();
                subscriptionDisposable.Dispose();
            });
        }
        public delegate void Handler(T value, Signal token);

        private Handler m_Handler;
        private Func<T, Signal, UniTaskVoid> m_AsyncHandler;
        // ReSharper disable once InconsistentNaming
        public Action<T> Handle { get; private set; }
    
        public SequentialHandler(Handler handler, Signal externalCancellationToken = default)
        {
            m_Handler = handler;
            externCancellationToken = externalCancellationToken;
            Handle = SyncHandle;
        }
        public SequentialHandler(Func<T, Signal, UniTaskVoid> handler, Signal externalCancellationToken = default)
        {
            m_AsyncHandler = handler;
            externCancellationToken = externalCancellationToken;
            Handle = AsyncHandle;
        }
        public void SyncHandle(T value)
        {
            Debug.Assert(!disposed, "SequentialHandler disposed but still used");
            ResetInternalCancellationSignal();
            m_Handler(value, internalCancellationTokenSource.Signal);
        }
        
        public void AsyncHandle(T value)
        {
            Debug.Assert(!disposed, "SequentialHandler disposed but still used");
            ResetInternalCancellationSignal();
            m_AsyncHandler(value, internalCancellationTokenSource.Signal).Forget();
        }

        protected override void EraseProps()
        {
            m_Handler = default;
        }
    }
    public class SequentialHandler<T1, T2>: SequentialHandlerBase
    {
        public static DisposableAction Subscribe(Func<Action<T1, T2>, IDisposable> subscribe, Handler handler)
        {
            var handlerDisposable = new SequentialHandler<T1, T2>(handler);
            var subscriptionDisposable = subscribe(handlerDisposable.Handle);
            return new DisposableAction(() =>
            {
                handlerDisposable.Dispose();
                subscriptionDisposable.Dispose();
            });
        }
        public delegate void Handler(T1 value1, T2 value2, Signal token);

        private Handler m_Handler;
    
        public SequentialHandler(Handler handler, Signal externalCancellationToken = default)
        {
            m_Handler = handler;
            externCancellationToken = externalCancellationToken;
        }
        public void Handle(T1 value1, T2 value2)
        {
            Debug.Assert(!disposed, "SequentialHandler disposed but still used");
            ResetInternalCancellationSignal();
            m_Handler(value1, value2, internalCancellationTokenSource.Signal);
        }

        protected override void EraseProps()
        {
            m_Handler = default;
        }
    }
    
    public class SequentialHandlerRef<T>: SequentialHandlerBase
    {
        public delegate void Handler(ref T value, Signal token);

        private Handler m_Handler;
    
        public SequentialHandlerRef(Handler handler, Signal externalCancellationToken = default)
        {
            m_Handler = handler;
            externCancellationToken = externalCancellationToken;
        }
        public void Handle(ref T value)
        {
            Debug.Assert(!disposed, "SequentialHandler disposed but still used");
            ResetInternalCancellationSignal();
            m_Handler(ref value, internalCancellationTokenSource.Signal);
        }

        protected override void EraseProps()
        {
            m_Handler = default;
        }
    }
    
    public class UniqueSequentialHandler<T>: SequentialHandlerBase
    {
        public delegate void Handler(T value, Signal token);

        private Handler m_Handler;
        private bool m_MemoizedValueSet;
        private T m_MemoizedValue;
    
        public UniqueSequentialHandler(Handler handler, Signal externalCancellationToken = default)
        {
            m_Handler = handler;
            externCancellationToken = externalCancellationToken;
        }
        public void Handle(T value)
        {
            Debug.Assert(!disposed, "SequentialHandler disposed but still used");
            if (m_MemoizedValueSet && m_MemoizedValue.Equals(value)) return;
            m_MemoizedValueSet = true;
            m_MemoizedValue = value;
            ResetInternalCancellationSignal();
            m_Handler(value, internalCancellationTokenSource.Signal);
        }

        protected override void EraseProps()
        {
            m_Handler = default;
        }
    }
}