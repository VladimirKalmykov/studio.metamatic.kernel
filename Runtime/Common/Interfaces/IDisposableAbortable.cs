using System;

namespace Metamatic
{
    /// <summary>
    /// Allows to dispose and abort the operation. Aborting and sync disposing is the same thing, but
    /// supporting aborting can be useful to support backward compatibility with the abort strategy.
    /// </summary>
    public interface IDisposableAbortable: IDisposable, IAbortable
    {
        
    }
}