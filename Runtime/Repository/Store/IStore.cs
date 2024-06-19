using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    public interface IStore : IDisposable
    {
        
    }
    public interface IStore<T>: IAsyncDisposable
    {
        public delegate T Modifier(T value);
        public delegate ref T ModifierRef(ref T value);
        public delegate T Merger(T valueFrom, T valueTo);
        public T value { get; set; }
        public long version { get; }
        public bool isDirty { get; }
        public UniTask WhenSynchronized(CancellationToken cancellationToken);
        public DisposableAction Checkout(string branchName, string sourceBranchName, bool createIfNotExists = false, bool removeAfterDispose = true);
        public DisposableAction Checkout(string branchName, bool createIfNotExists = false, bool removeAfterDispose = true);
        public T Get();
        public void Set(T value);
        public UniTask<T> GetPristineValue(CancellationToken cancellationToken);
        public void Modify(Modifier action);
        public void ModifyRef(ModifierRef action);
        public void Populate(T value);
        public void Populate(Modifier modifier);
        public DisposableAction Subscribe(Action<T> action);
        public void Merge(string fromBranch, string toBranch, Merger merger);
        public void Merge(string fromBranch, Merger merger);
        public void Merge(Merger merger);
        public UniTask Commit(CancellationToken cancellationToken);
        public UniTask Sync(CancellationToken cancellationToken);
        public UniTask Reset(bool populate, CancellationToken cancellationToken);
        public UniTask Reset(CancellationToken cancellationToken);
        public UniTask<IDisposable> Lock(CancellationToken cancellationToken);
    }
}