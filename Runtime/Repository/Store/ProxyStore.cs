using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    public class ProxyStore<T>: IStore<T>
    {
        private readonly Store<T> store;
        private bool m_Disposed;
        private event Action OnDispose;
        public ProxyStore(Store<T> store)
        {
            this.store = store;
            OnDispose += store.Inquire();
        }
        
        ~ProxyStore()
        {
            Dispose(true, false);
        }

        public UniTask WhenSynchronized(CancellationToken cancellationToken)
        {
            return store.WhenSynchronized(cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DisposableAction AddBranch(string name, bool makeCurrent = false)
        {
            return store.AddBranch(name, makeCurrent);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CheckoutBranch(string name)
        {
            store.CheckoutBranch(name);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(T value)
        {
            store.Set(value);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Get()
        {
            return store.Get();
        }

        public DisposableAction Subscribe(Action<T> action)
        {
            return store.Subscribe(action);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UniTask Commit(string branchName, CancellationToken cancellationToken)
        {
            return store.Commit(branchName, cancellationToken);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UniTask Commit(CancellationToken cancellationToken)
        {
            return store.Commit(cancellationToken);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UniTask Sync(CancellationToken cancellationToken)
        {
            return store.Sync(cancellationToken);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UniTask Reset(bool populate, CancellationToken cancellationToken)
        {
            return store.Reset(populate, cancellationToken);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UniTask Reset(CancellationToken cancellationToken)
        {
            return store.Reset(cancellationToken);
        }
        
        public void Dispose()
        {
            Dispose(false, false);
        }
        
        public void Dispose(bool isCalledFromFinalizer, bool isAsync)
        {
            if (isCalledFromFinalizer)
            {
                throw new InvalidOperationException("Dispose should be called explicitly, not from finalizer.");
            }
            if (m_Disposed) return;
            m_Disposed = true;
            OnDispose?.Invoke();
            GC.SuppressFinalize(this);
        }

        public async ValueTask DisposeAsync()
        {
            if (m_Disposed) return;
            Dispose(false, true);
            if (store.isDisposed) await store.asyncDisposingTask;
        }

        
    }
}