using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    public interface IStore<T>: IDisposable, IAsyncDisposable
    {
        public UniTask WhenSynchronized(CancellationToken cancellationToken);
        public DisposableAction AddBranch(string name, bool makeCurrent = false);
        public void CheckoutBranch(string name);
        public void Set(T value);
        public T Get();
        public DisposableAction Subscribe(Action<T> action);
        public UniTask Commit(string branchName, CancellationToken cancellationToken);
        public UniTask Commit(CancellationToken cancellationToken);
        public UniTask Sync(CancellationToken cancellationToken);
        public UniTask Reset(bool populate, CancellationToken cancellationToken);
        public UniTask Reset(CancellationToken cancellationToken);
    }
}