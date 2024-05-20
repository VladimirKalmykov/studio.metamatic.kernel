using System;
using System.Threading;
using UnityEngine;

namespace Metamatic
{
    public abstract class ExclusiveHandlerBase: IDisposable
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
    public class ExclusiveExecution: ExclusiveHandlerBase
    {
        public delegate void Handler(CancellationToken token);

        private Handler m_Handler;
    
        public ExclusiveExecution(Handler handler, CancellationToken externalCancellationToken = default)
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
    public class ExclusiveExecution<T>: ExclusiveHandlerBase
    {
        public delegate void Handler(T value, CancellationToken token);

        private Handler m_Handler;
    
        public ExclusiveExecution(Handler handler, CancellationToken externalCancellationToken = default)
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
    public class ExclusiveExecution<T1, T2>: ExclusiveHandlerBase
    {
        public delegate void Handler(T1 value1, T2 value2, CancellationToken token);

        private Handler m_Handler;
    
        public ExclusiveExecution(Handler handler, CancellationToken externalCancellationToken = default)
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

    public class ExclusiveExecution<T1, T2, T3> : ExclusiveHandlerBase
    {
        public delegate void Handler(T1 value1, T2 value2, T3 value3, CancellationToken token);

        private Handler m_Handler;

        public ExclusiveExecution(Handler handler, CancellationToken externalCancellationToken = default)
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
    
    public class ExclusiveExecution<T1, T2, T3, T4> : ExclusiveHandlerBase
    {
        public delegate void Handler(T1 value1, T2 value2, T3 value3, T4 value4, CancellationToken token);

        private Handler m_Handler;

        public ExclusiveExecution(Handler handler, CancellationToken externalCancellationToken = default)
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
    
    public class ExclusiveExecution<T1, T2, T3, T4, T5> : ExclusiveHandlerBase
    {
        public delegate void Handler(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, CancellationToken token);

        private Handler m_Handler;

        public ExclusiveExecution(Handler handler, CancellationToken externalCancellationToken = default)
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
    
    public class ExclusiveExecutionRef<T>: ExclusiveHandlerBase
    {
        public delegate void Handler(ref T value, CancellationToken token);

        private Handler m_Handler;
    
        public ExclusiveExecutionRef(Handler handler, CancellationToken externalCancellationToken = default)
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
}