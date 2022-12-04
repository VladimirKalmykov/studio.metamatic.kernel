using System;
using System.Runtime.CompilerServices;

namespace Metamatic
{
    public class Supply: IDisposable, ICriticalNotifyCompletion
    {
        public readonly float priority;
        public event Action OnDispose;
        private bool m_Disposed;

        public Supply(float priority = 0f)
        {
            this.priority = priority;
        }
        public void Dispose()
        {
            if (m_Disposed) return;
            m_Disposed = true;
            OnDispose?.Invoke();
        }

        public virtual void Release()
        {
            Dispose();
        }

        public void OnCompleted(Action continuation)
        {
            OnDispose += continuation;
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            OnDispose += continuation;
        }
    }

    public class Offer<T> : Supply
    {
        public readonly T value;
        public Offer(T value, float priority = 0f): base(priority)
        {
            this.value = value;
        }

        public override void Release()
        {
            
        }
    }
}