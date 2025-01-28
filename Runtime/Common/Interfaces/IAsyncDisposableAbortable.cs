using Cysharp.Threading.Tasks;

namespace Metamatic
{
    public interface IAsyncDisposableAbortable: IUniTaskAsyncDisposable, IAbortable
    {
        public bool isDisposedAsync { get; }
    }
}