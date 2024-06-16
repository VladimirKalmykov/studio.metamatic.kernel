using System;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    public class NetworkOperation: IDisposable
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
        
        public readonly Storage source;
        public readonly StorageRequestConfig config;
        public MicroState<Status> m_Status = new MicroState<Status>(Status.Idle);
        protected bool m_IsDisposed { get; private set; }

        public Status status
        {
            get => m_Status.value;
            set => m_Status.value = value;
        }
            
        public NetworkOperation(Storage source, StorageRequestConfig config)
        {
            this.source = source;
            this.config = config.Retry();
        }

        public virtual void Dispose()
        {
            m_IsDisposed = true;
        }
    }
    
    public class NetworkReadOperation<T> : NetworkOperation
    {
        public UniTask<(T, Storage.Metadata)> task;
        public NetworkReadOperation(Storage source, StorageRequestConfig config) : base(source, config)
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
    
    public class NetworkWriteOperation<T> : NetworkOperation
    {
        public readonly T data;
        public UniTask task;
        public NetworkWriteOperation(Storage source, StorageRequestConfig config, T data) : base(source, config)
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