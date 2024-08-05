using System.Threading;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    using System;

    public interface IPool: IDisposable
    {
#if DEBUG
        public string debugUniqueId { get; }
#endif
        CancellationToken disposeCancellationToken { get; }
        /// <summary>
        /// Wait until all created objects are released. It uses simple math to calculate spawned and released objects.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        UniTask WhenAllReleased(CancellationToken cancellationToken);
    }
    
    public interface IUniversalPool<T>: IPool
    {
        T Get();
        void Release(T obj);
        void TryRelease(T obj);
    }

    public interface IUnityObjectPool<T> : IPool where T : UnityEngine.Object
    {
        T Get();
        void Release(T obj);
        void TryRelease(T obj);
    }
}