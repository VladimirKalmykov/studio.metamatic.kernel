using System;

namespace Metamatic
{
    /// <summary>
    /// 
    /// </summary>
    public interface IExposedDisposed
    {
        bool isDisposed { get; }
        event Action OnDisposed;
    }
    
    public interface IExposedAsyncDisposed
    {
        bool isDisposedAsync { get; }
        event Action<Exception> OnDisposedAsync;
    }
}