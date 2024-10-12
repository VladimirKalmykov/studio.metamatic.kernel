using System;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    public interface IEventLoop<T>
    where T: System.Enum
    {
        public UniTask WaitUntil(T eventType, Func<float, bool> action);
    }
}