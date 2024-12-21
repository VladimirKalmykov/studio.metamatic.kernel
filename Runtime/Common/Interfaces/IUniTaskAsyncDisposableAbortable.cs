using Cysharp.Threading.Tasks;

namespace Metamatic
{
    public interface IUniTaskDisposedAsyncDisposableAbortable: IAbortable, IUniTaskAsyncDisposable, IExposedDisposedAsync
    {
        
    }
}