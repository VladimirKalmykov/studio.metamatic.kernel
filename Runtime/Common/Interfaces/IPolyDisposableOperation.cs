using Cysharp.Threading.Tasks;

namespace Metamatic
{
    public interface IPolyDisposableOperation: IUniTaskAsyncDisposable, System.IDisposable, IExposedDisposedAsync, IExposedDisposedSync
    {
        
    }
}