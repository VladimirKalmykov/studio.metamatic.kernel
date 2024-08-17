using System;
using System.Runtime.CompilerServices;
using System.Threading;
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
        public T value
        {
            get => store.value;
            set => store.value = value;
        }
        
        ~ProxyStore()
        {
            Dispose(true, false);
        }

        public long version => store.version;
        public bool isDirty => store.isDirty;

        public UniTask WhenSynchronized(CancellationToken cancellationToken)
        {
            return store.WhenSynchronized(cancellationToken);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DisposableAction Checkout(string branchName, string sourceBranchName, bool createIfNotExists = false, bool removeAfterDispose = true)
        {
            return store.Checkout(branchName, sourceBranchName, createIfNotExists, removeAfterDispose);
        }

        public DisposableAction Checkout(string branchName, bool createIfNotExists = false, bool removeAfterDispose = true)
        {
            return store.Checkout(branchName, createIfNotExists, removeAfterDispose);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(T value)
        {
            store.Set(value);
        }

        public UniTask<T> GetPristineValue(CancellationToken cancellationToken)
        {
            return store.GetPristineValue(cancellationToken);
        }

        public void Modify(IStore<T>.Modifier action)
        {
            store.Modify(action);
        }

        public void ModifyRef(IStore<T>.ModifierRef action)
        {
            store.ModifyRef(action);
        }

        public void Populate(T value)
        {
            store.Populate(value);
        }

        public void Populate(IStore<T>.Modifier modifier)
        {
            store.Populate(modifier);
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

        public void Merge(string fromBranch, string toBranch, IStore<T>.Merger merger)
        {
            store.Merge(fromBranch, toBranch, merger);
        }

        public void Merge(string fromBranch, IStore<T>.Merger merger)
        {
            store.Merge(fromBranch, merger);
        }

        public void Merge(IStore<T>.Merger merger)
        {
            store.Merge(merger);
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
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UniTask<IDisposable> Lock(CancellationToken cancellationToken)
        {
            return store.Lock(cancellationToken);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DisposableAction SubscribeDispose(Action action)
        {
            return store.SubscribeDispose(action);
        }

        public void Dispose(bool isCalledFromFinalizer = false, bool isAsync = false)
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

        public async UniTask DisposeAsync()
        {
            if (m_Disposed) return;
            Dispose(false, true);
            if (store.isDisposed) await store.asyncDisposingTask;
        }

        
    }
}