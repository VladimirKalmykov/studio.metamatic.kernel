using System;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    public readonly struct AsyncDisposableOperation: IDisposableOperation, IExposedAsyncDisposed, IExposedDisposed
    {
        /// <summary>
        /// Call an async action with cancellation token
        /// </summary>
        /// <param name="action">Async action to execute</param>
        /// <param name="dependencies">Array of dependencies. Disposed is sync if any of the dependencies equals null</param>
        /// <returns></returns>
        public static AsyncDisposableOperation Call(InternalMicroOperation.AsyncAction action, object[] dependencies = null)
        {
            return InternalMicroOperation.Call(action, dependencies);
        }

        /// <summary>
        /// Run an async operation
        /// </summary>
        /// <param name="action">Async action to execute</param>
        /// <param name="dependencies">Array of dependencies. Disposed is sync if any of the dependencies equals null</param>
        /// <param name="debug">Debug operation (use this in development purposes)</param>
        /// <returns></returns>
        public static AsyncDisposableOperation Run(InternalMicroOperation.AsyncOperationAction action, object[] dependencies = null, bool debug = false)
        {
            return InternalMicroOperation.Run(action, dependencies, debug);
        }
        
        private readonly InternalMicroOperation m_Operation;
        public readonly int InquireId;
        public bool isInitialized => m_Operation != null;
        public bool isAlive => m_Operation != null && m_Operation.inquireId == InquireId;
        public bool isDisposedAsync => m_Operation.isDisposedAsync;
        public bool isDisposed => m_Operation.isDisposed;
        public event Action OnDisposed { add => m_Operation.OnDisposed += value; remove => m_Operation.OnDisposed -= value; }
        public event Action<Exception> OnDisposedAsync { add => m_Operation.OnDisposedAsync += value; remove => m_Operation.OnDisposedAsync -= value; }
        public object[] dependencies => m_Operation.dependencies;
        
        internal AsyncDisposableOperation(InternalMicroOperation operation)
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
        
        public static implicit operator DisposableAction(AsyncDisposableOperation operation)
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