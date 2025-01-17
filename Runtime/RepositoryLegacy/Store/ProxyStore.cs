using System;
using System.Runtime.CompilerServices;
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

        public UniTask WhenSynchronized(Signal signal)
        {
            return store.WhenSynchronized(signal);
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

        public UniTask<T> GetPristineValue(Signal signal)
        {
            return store.GetPristineValue(signal);
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
        public UniTask Commit(Signal signal)
        {
            return store.Commit(signal);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UniTask Sync(Signal signal)
        {
            return store.Sync(signal);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UniTask Reset(bool populate, Signal cancellationSignal)
        {
            return store.Reset(populate, cancellationSignal);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UniTask Reset(Signal cancellationSignal)
        {
            return store.Reset(cancellationSignal);
        }
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UniTask<IDisposable> Lock(Signal signal)
        {
            return store.Lock(signal);
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