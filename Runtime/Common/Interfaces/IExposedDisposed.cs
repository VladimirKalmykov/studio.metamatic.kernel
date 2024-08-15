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
}