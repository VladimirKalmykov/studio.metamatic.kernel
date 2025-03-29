using System;
using Cysharp.Threading.Tasks;
using Metamatic;

namespace Metamatic
{
    /// <summary>
    /// Simple disposable async operation that can be disposed synchronously.
    /// It is foundation struct for async programming with disposable operations.
    /// </summary>
    public readonly struct DisposableOperation: IDisposable
    {
        private static readonly Func<Signal, UniTask> EmptyOperation = _ => UniTask.CompletedTask;
        // ReSharper disable once InconsistentNaming
        public static DisposableOperation Empty => new DisposableOperation(EmptyOperation);
        public static DisposableOperation Call(Func<Signal, UniTask> operation)
        {
            return new DisposableOperation(operation);
        }
        private readonly SignalSource m_CancellationSignalSource;
        public DisposableOperation(Func<Signal, UniTask> operation)
        {
            m_CancellationSignalSource = SignalSource.Create();
            operation(m_CancellationSignalSource.Signal).Forget();
        }

        public void Dispose()
        {
            m_CancellationSignalSource.TryRaise();
        }
        
        public static explicit operator DisposableAction(DisposableOperation disposableOperation)
        {
            return new DisposableAction(disposableOperation.Dispose);
        }
    }
}