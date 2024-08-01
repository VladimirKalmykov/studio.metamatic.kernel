using System;
using System.Threading;
using UnityEngine;

namespace Metamatic
{
    public abstract class SequentialHandlerBase: IDisposable
    {
        protected CancellationToken externCancellationToken;
        protected CancellationTokenSource internalCancellationTokenSource;
        public bool disposed { get; private set; }
        protected void CancelInternalToken()
        {
            if (internalCancellationTokenSource == null) return;
            if (internalCancellationTokenSource.IsCancellationRequested) return;
            internalCancellationTokenSource?.Cancel();
            internalCancellationTokenSource?.Dispose();
            internalCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(externCancellationToken);
        }
        public void Dispose()
        {
            disposed = true;
            internalCancellationTokenSource?.Cancel();
            internalCancellationTokenSource?.Dispose();
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
        public delegate void Handler(CancellationToken token);

        private Handler m_Handler;
    
        public SequentialHandler(Handler handler, CancellationToken externalCancellationToken = default)
        {
            m_Handler = handler;
            externCancellationToken = externalCancellationToken;
        }
        public void Handle()
        {
            Debug.Assert(!disposed, "SequentialHandler disposed but still used");
            Debug.Assert(m_Handler != null, "Handler is null");
            CancelInternalToken();
            internalCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(externCancellationToken);
            m_Handler(internalCancellationTokenSource.Token);
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
        public delegate void Handler(T value, CancellationToken token);

        private Handler m_Handler;
    
        public SequentialHandler(Handler handler, CancellationToken externalCancellationToken = default)
        {
            m_Handler = handler;
            externCancellationToken = externalCancellationToken;
        }
        public void Handle(T value)
        {
            Debug.Assert(!disposed, "SequentialHandler disposed but still used");
            CancelInternalToken();
            internalCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(externCancellationToken);
            m_Handler(value, internalCancellationTokenSource.Token);
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
        public delegate void Handler(T1 value1, T2 value2, CancellationToken token);

        private Handler m_Handler;
    
        public SequentialHandler(Handler handler, CancellationToken externalCancellationToken = default)
        {
            m_Handler = handler;
            externCancellationToken = externalCancellationToken;
        }
        public void Handle(T1 value1, T2 value2)
        {
            Debug.Assert(!disposed, "SequentialHandler disposed but still used");
            CancelInternalToken();
            internalCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(externCancellationToken);
            m_Handler(value1, value2, internalCancellationTokenSource.Token);
        }

        protected override void EraseProps()
        {
            m_Handler = default;
        }
    }

    public class SequentialHandler<T1, T2, T3> : SequentialHandlerBase
    {
        public static DisposableAction Subscribe(Func<Action<T1, T2, T3>, IDisposable> subscribe, Handler handler)
        {
            var handlerDisposable = new SequentialHandler<T1, T2, T3>(handler);
            var subscriptionDisposable = subscribe(handlerDisposable.Handle);
            return new DisposableAction(() =>
            {
                handlerDisposable.Dispose();
                subscriptionDisposable.Dispose();
            });
        }
        public delegate void Handler(T1 value1, T2 value2, T3 value3, CancellationToken token);

        private Handler m_Handler;

        public SequentialHandler(Handler handler, CancellationToken externalCancellationToken = default)
        {
            m_Handler = handler;
            externCancellationToken = externalCancellationToken;
        }

        public void Handle(T1 value1, T2 value2, T3 value3)
        {
            Debug.Assert(!disposed, "SequentialHandler disposed but still used");
            CancelInternalToken();
            internalCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(externCancellationToken);
            m_Handler(value1, value2, value3, internalCancellationTokenSource.Token);
        }

        protected override void EraseProps()
        {
            m_Handler = default;
        }
    }
    
    public class SequentialHandler<T1, T2, T3, T4> : SequentialHandlerBase
    {
        public static DisposableAction Subscribe(Func<Action<T1, T2, T3, T4>, IDisposable> subscribe, Handler handler)
        {
            var handlerDisposable = new SequentialHandler<T1, T2, T3, T4>(handler);
            var subscriptionDisposable = subscribe(handlerDisposable.Handle);
            return new DisposableAction(() =>
            {
                handlerDisposable.Dispose();
                subscriptionDisposable.Dispose();
            });
        }
        public delegate void Handler(T1 value1, T2 value2, T3 value3, T4 value4, CancellationToken token);

        private Handler m_Handler;

        public SequentialHandler(Handler handler, CancellationToken externalCancellationToken = default)
        {
            m_Handler = handler;
            externCancellationToken = externalCancellationToken;
        }

        public void Handle(T1 value1, T2 value2, T3 value3, T4 value4)
        {
            Debug.Assert(!disposed, "SequentialHandler disposed but still used");
            CancelInternalToken();
            internalCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(externCancellationToken);
            m_Handler(value1, value2, value3, value4, internalCancellationTokenSource.Token);
        }

        protected override void EraseProps()
        {
            m_Handler = default;
        }
    }
    
    public class SequentialHandler<T1, T2, T3, T4, T5> : SequentialHandlerBase
    {
        public static DisposableAction Subscribe(Func<Action<T1, T2, T3, T4, T5>, IDisposable> subscribe, Handler handler)
        {
            var handlerDisposable = new SequentialHandler<T1, T2, T3, T4, T5>(handler);
            var subscriptionDisposable = subscribe(handlerDisposable.Handle);
            return new DisposableAction(() =>
            {
                handlerDisposable.Dispose();
                subscriptionDisposable.Dispose();
            });
        }
        public delegate void Handler(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, CancellationToken token);

        private Handler m_Handler;

        public SequentialHandler(Handler handler, CancellationToken externalCancellationToken = default)
        {
            m_Handler = handler;
            externCancellationToken = externalCancellationToken;
        }

        public void Handle(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5)
        {
            Debug.Assert(!disposed, "SequentialHandler disposed but still used");
            CancelInternalToken();
            internalCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(externCancellationToken);
            m_Handler(value1, value2, value3, value4, value5, internalCancellationTokenSource.Token);
        }

        protected override void EraseProps()
        {
            m_Handler = default;
        }
    }
    
    public class SequentialHandlerRef<T>: SequentialHandlerBase
    {
        public delegate void Handler(ref T value, CancellationToken token);

        private Handler m_Handler;
    
        public SequentialHandlerRef(Handler handler, CancellationToken externalCancellationToken = default)
        {
            m_Handler = handler;
            externCancellationToken = externalCancellationToken;
        }
        public void Handle(ref T value)
        {
            Debug.Assert(!disposed, "SequentialHandler disposed but still used");
            CancelInternalToken();
            internalCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(externCancellationToken);
            m_Handler(ref value, internalCancellationTokenSource.Token);
        }

        protected override void EraseProps()
        {
            m_Handler = default;
        }
    }
    
    public class UniqueSequentialHandler<T>: SequentialHandlerBase
    {
        public delegate void Handler(T value, CancellationToken token);

        private Handler m_Handler;
        private bool m_MemoizedValueSet;
        private T m_MemoizedValue;
    
        public UniqueSequentialHandler(Handler handler, CancellationToken externalCancellationToken = default)
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
            CancelInternalToken();
            internalCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(externCancellationToken);
            m_Handler(value, internalCancellationTokenSource.Token);
        }

        protected override void EraseProps()
        {
            m_Handler = default;
        }
    }
}