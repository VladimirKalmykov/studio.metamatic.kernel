using System;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    public class IOOperation: IDisposable
    {
        public Type type = default;
        public enum Status
        {
            Idle,
            Pending,
            Error,
            Done,
            Cancelled,
            Timeout,
            Retrying,
            Rejected
        }
        public readonly IOOperationConfig config;
        public MicroState<Status> m_Status = new MicroState<Status>(Status.Idle);
        protected bool m_IsDisposed { get; private set; }

        public Status status
        {
            get => m_Status.value;
            set => m_Status.value = value;
        }
            
        public IOOperation(IOOperationConfig config)
        {
            this.config = config.Retry();
        }

        public virtual void Dispose()
        {
            m_IsDisposed = true;
        }
    }
    
    public class IOReadOperation<T> : IOOperation
    {
        public UniTask<T> task;
        public IOReadOperation(IOOperationConfig config) : base(config)
        {
            type = typeof(T);
        }

        public override void Dispose()
        {
            if (m_IsDisposed) return;
            task = default;
            base.Dispose();
        }
    }
    
    public class IOWriteOperation<T> : IOOperation
    {
        public readonly T data;
        public UniTask task;
        public IOWriteOperation(IOOperationConfig config, T data) : base(config)
        {
            type = typeof(T);
            this.data = data;
        }
        
        public override void Dispose()
        {
            if (m_IsDisposed) return;
            task = default;
            base.Dispose();
        }
    }
}