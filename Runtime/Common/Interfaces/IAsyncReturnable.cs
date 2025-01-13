using Cysharp.Threading.Tasks;

namespace Metamatic
{
    public interface IAsyncReturnable: IAbortable
    {
        public bool isReturning { get; }
        public UniTask ReturnAsync();
    }
}