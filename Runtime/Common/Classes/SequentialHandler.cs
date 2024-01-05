using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic
{
    public abstract class SequentialHandler: IDisposable
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
            internalCancellationTokenSource?.Dispose();
            EraseProps();
        }

        protected abstract void EraseProps();
    }
    public class SequentialHandler<TContext>: SequentialHandler
    {
        public delegate void Handler(CancellationToken token, TContext context);

        private Handler m_Handler;
        private TContext m_Context;
        
        public SequentialHandler(Handler handler, CancellationToken externalCancellationToken, TContext context = default)
        {
            m_Handler = handler;
            m_Context = context;
            externCancellationToken = externalCancellationToken;
        }
        public void Handle()
        {
            Debug.Assert(!disposed, "SequentialHandler disposed but still used");
            CancelInternalToken();
            internalCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(externCancellationToken);
            m_Handler(internalCancellationTokenSource.Token, m_Context);
        }

        protected override void EraseProps()
        {
            m_Handler = default;
            m_Context = default;
        }
    }
    public class SequentialHandler<T, TContext>: SequentialHandler
    {
        public delegate void Handler(T value, CancellationToken token, TContext context);

        private Handler m_Handler;
        private TContext m_Context;
        
        public SequentialHandler(Handler handler, CancellationToken externalCancellationToken, TContext context = default)
        {
            m_Handler = handler;
            m_Context = context;
            externCancellationToken = externalCancellationToken;
        }
        public void Handle(T value)
        {
            Debug.Assert(!disposed, "SequentialHandler disposed but still used");
            CancelInternalToken();
            internalCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(externCancellationToken);
            m_Handler(value, internalCancellationTokenSource.Token, m_Context);
        }

        protected override void EraseProps()
        {
            m_Handler = default;
            m_Context = default;
        }
    }
    
    public class SequentialHandler<T1, T2, TContext>: SequentialHandler
    {
        public delegate void Handler(T1 value1, T2 value2, CancellationToken token, TContext context);

        private Handler m_Handler;
        private TContext m_Context;
        
        public SequentialHandler(Handler handler, CancellationToken externalCancellationToken, TContext context = default)
        {
            m_Handler = handler;
            m_Context = context;
            externCancellationToken = externalCancellationToken;
        }
        public void Handle(T1 value1, T2 value2)
        {
            Debug.Assert(!disposed, "SequentialHandler disposed but still used");
            CancelInternalToken();
            internalCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(externCancellationToken);
            m_Handler(value1, value2, internalCancellationTokenSource.Token, m_Context);
        }

        protected override void EraseProps()
        {
            m_Handler = default;
            m_Context = default;
        }
    }
    
    public class SequentialHandlerRef<T, TContext>: SequentialHandler
    {
        public delegate void Handler(ref T value, CancellationToken token, TContext context);

        private Handler m_Handler;
        private TContext m_Context;
        
        public SequentialHandlerRef(Handler handler, CancellationToken externalCancellationToken, TContext context = default)
        {
            m_Handler = handler;
            m_Context = context;
            externCancellationToken = externalCancellationToken;
        }
        public void Handle(ref T value)
        {
            Debug.Assert(!disposed, "SequentialHandler disposed but still used");
            CancelInternalToken();
            internalCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(externCancellationToken);
            m_Handler(ref value, internalCancellationTokenSource.Token, m_Context);
        }

        protected override void EraseProps()
        {
            m_Handler = default;
            m_Context = default;
        }
    }
    
    public class SequentialHandlerRef<T1, T2, TContext>: SequentialHandler
    {
        public delegate void Handler(ref T1 value1, ref T2 value2, CancellationToken token, TContext context);

        private Handler m_Handler;
        private TContext m_Context;
        
        public SequentialHandlerRef(Handler handler, CancellationToken externalCancellationToken, TContext context = default)
        {
            m_Handler = handler;
            m_Context = context;
            externCancellationToken = externalCancellationToken;
        }
        public void Handle(ref T1 value1, ref T2 value2)
        {
            Debug.Assert(!disposed, "SequentialHandler disposed but still used");
            CancelInternalToken();
            internalCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(externCancellationToken);
            m_Handler(ref value1, ref value2, internalCancellationTokenSource.Token, m_Context);
        }

        protected override void EraseProps()
        {
            m_Handler = default;
            m_Context = default;
        }
    }
}