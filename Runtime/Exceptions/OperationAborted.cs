using System;

namespace Metamtic.Exceptions
{
    /// <summary>
    /// Derived from class OperationCanceledException this exception is thrown when operation is aborted
    /// </summary>
    public class OperationAborted: OperationCanceledException
    {
        public readonly object ExperimentalSource;
        public OperationAborted(string message, object source = null) : base(message)
        {
            this.ExperimentalSource = source;
        }
    }
}