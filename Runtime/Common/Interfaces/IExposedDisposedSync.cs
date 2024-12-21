using System;

namespace Metamatic
{
    /// <summary>
    /// 
    /// </summary>
    public interface IExposedDisposedSync
    {
        bool isDisposedSync { get; }
        event Action OnDisposedSync;
    }
    
    public interface IExposedDisposedAsync
    {
        bool isDisposedAsync { get; }
        event Action<Exception> OnDisposedAsync;
    }
}