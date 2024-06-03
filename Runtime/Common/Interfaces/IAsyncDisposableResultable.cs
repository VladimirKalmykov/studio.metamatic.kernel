using System;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    public interface IAsyncDisposableResultable: IAsyncDisposable
    {
        public UniTask result { get; }
    }
}