using System;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    public interface IPolyDisposable: IDisposable, IUniTaskAsyncDisposable
    {
        
    }
}