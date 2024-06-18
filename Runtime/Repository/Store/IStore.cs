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
        public T value { get; set; }
        public UniTask WhenSynchronized(CancellationToken cancellationToken);
        public DisposableAction AddBranch(string name, bool makeCurrent = false);
        public void CheckoutBranch(string name);
        public T Get();
        public void Set(T value);
        public void Modify(Modifier action);
        public void ModifyRef(ModifierRef action);
        public DisposableAction Subscribe(Action<T> action);
        public UniTask Commit(string branchName, CancellationToken cancellationToken);
        public UniTask Commit(CancellationToken cancellationToken);
        public UniTask Sync(CancellationToken cancellationToken);
        public UniTask Reset(bool populate, CancellationToken cancellationToken);
        public UniTask Reset(CancellationToken cancellationToken);
    }
}