using System;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    public interface IAsyncDisposableResultable: IAsyncDisposable
    {
        public UniTask result { get; }
    }
    
    public interface IAsyncDisposableResultable<T>: IAsyncDisposable
    {
        public new UniTask<T> result { get; }
    }
}