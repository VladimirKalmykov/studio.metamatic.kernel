using Cysharp.Threading.Tasks;

namespace Metamatic
{
    public interface IAsyncDisposableResultable: IUniTaskAsyncDisposable
    {
        public UniTask result { get; }
    }
    
    public interface IAsyncDisposableResultable<T>: IUniTaskAsyncDisposable
    {
        public new UniTask<T> result { get; }
    }
}