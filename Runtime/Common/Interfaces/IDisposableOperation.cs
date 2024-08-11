using System;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    public interface IDisposableOperation: IUniTaskAsyncDisposable
    {
        public void Dispose();
    }
}