using System;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    public interface IUniTaskAsyncDisposableAbortable: IAbortable, IUniTaskAsyncDisposable, IExposedAsyncDisposed
    {
        
    }
}