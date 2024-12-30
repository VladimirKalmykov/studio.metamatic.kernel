using Cysharp.Threading.Tasks;

namespace Metamatic
{
    // TODO: DO we need this interface?
    public interface IUniTaskDisposedAsyncDisposableAbortable: IAbortable, IUniTaskAsyncDisposable, IExposedDisposedAsync
    {
        
    }
}