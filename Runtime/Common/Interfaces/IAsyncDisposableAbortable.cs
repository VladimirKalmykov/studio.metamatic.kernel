using Cysharp.Threading.Tasks;

namespace Metamatic
{
    public interface IAsyncDisposableAbortable: IUniTaskAsyncDisposable, IAbortable
    {
        
    }
}