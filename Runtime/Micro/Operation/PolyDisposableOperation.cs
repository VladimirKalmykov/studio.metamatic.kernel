using System;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    /// <summary>
    /// Allows to run async operations with dependencies and dispose them synchronously or asynchronously.
    /// Depending on disposing type, the operation will be cancelled synchronously or asynchronously and you can handle the disposing event and type.
    /// </summary>
    /// <example>
    /// PolyDisposableOperation.Call(async operation => {
    ///    operation.Within(Hint.Create("Loading..."));
    ///    await operation.cancellationSignal; // Wait for the operation to be disposed
    ///    if (operation.cancellationType == CancellationType.Async) {
    ///     await hint.DisposeAsync(); // Dispose the hint asynchronously        
    ///    } else {
    ///         
    ///    }
    /// 
    /// });
    /// </example>
    public readonly struct PolyDisposableOperation: IPolyDisposableOperation
    {
        /// <summary>
        /// Call an async action with cancellation token
        /// </summary>
        /// <param name="action">Async action to execute</param>
        /// <param name="dependencies">Array of dependencies. Disposed is sync if any of the dependencies equals null</param>
        /// <returns></returns>
        public static PolyDisposableOperation Call(InternalAsyncDisposableOperation.AsyncAction action, object[] dependencies = null)
        {
            return InternalAsyncDisposableOperation.Call(action, dependencies);
        }

        /// <summary>
        /// Run an async operation
        /// </summary>
        /// <param name="action">Async action to execute</param>
        /// <param name="dependencies">Array of dependencies. Disposed is sync if any of the dependencies equals null</param>
        /// <param name="debug">Debug operation (use this in development purposes)</param>
        /// <returns></returns>
        public static PolyDisposableOperation Run(InternalAsyncDisposableOperation.AsyncOperationAction action, object[] dependencies = null, bool debug = false)
        {
            return InternalAsyncDisposableOperation.Run(action, dependencies, debug);
        }
        
        private readonly InternalAsyncDisposableOperation m_Operation;
        public readonly int InquireId;
        public bool isInitialized => m_Operation != null;
        public bool isAlive => m_Operation != null && m_Operation.inquireId == InquireId;
        public bool isDisposedAsync => m_Operation.isDisposedAsync;
        public bool isDisposedSync => m_Operation.isDisposedSync;
        public event Action OnDisposedSync { add => m_Operation.OnDisposedSync += value; remove => m_Operation.OnDisposedSync -= value; }
        public event Action<Exception> OnDisposedAsync { add => m_Operation.OnDisposedAsync += value; remove => m_Operation.OnDisposedAsync -= value; }
        public object[] dependencies => m_Operation.dependencies;
        
        internal PolyDisposableOperation(InternalAsyncDisposableOperation operation)
        {
            m_Operation = operation;
            InquireId = m_Operation.Inquire();
        }

        private UniTask AnyDispose(bool ignoreExceptions = false)
        {
            return m_Operation?.Release(InquireId, ignoreExceptions) ?? UniTask.CompletedTask;
        }

        public void Dispose()
        {
            m_Operation?.Abort(InquireId);
        }
        
        public static implicit operator DisposableAction(PolyDisposableOperation operation)
        {
            return new DisposableAction(operation.Dispose);
        }

        public IDisposable ToIDisposable()
        {
            return new DisposableAction(Dispose);
        }

        public UniTask DisposeAsync()
        {
            return AnyDispose(true);
        }
        
        /// <summary>
        /// Dispose the operation synchronously. Ignores any exceptions.
        /// </summary>
        /// <returns></returns>
        public void TryDispose()
        {
            AnyDispose(true).Forget();
        }
    }
}