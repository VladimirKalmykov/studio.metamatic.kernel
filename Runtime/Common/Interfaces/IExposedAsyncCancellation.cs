namespace Metamatic
{
    /// <summary>
    /// Object that can be disposed synchronously and asynchronously. When disposed, its cancellation signal is triggered.
    /// To determine how exactly object was disposed, check the isDisposedSync and isDisposedAsync properties (or disposedType property).
    /// </summary>
    /// <remarks>
    /// This interface is used to expose the cancellation signal of an object that can be disposed synchronously and asynchronously.
    /// It is useful when you need to run some operation within the async operation and need to know when the object is disposed.
    /// For example, you need to draw a health indicator above the character. While the character is alive, the health indicator should be drawn,
    /// but when the character is dead, the health indicator should be removed softly (e.g. by fade in). But if the character is removed from the scene,
    /// you should remove the health indicator immediately. In this case, you can check disposedType properties to determine how to remove the health indicator.
    /// </remarks>
    /// <example>
    /// async UniTaskVoid RunWithinAsyncOperation(IExposedAsyncCancellation asyncCancellation) {
    ///     try {
    ///       await asyncCancellation.cancellationSignal;
    ///     } finally {
    ///         if (asyncCancellation.isDisposedSync) {
    ///             // Object is disposed synchronously, release resources immediately
    ///         } else if (asyncCancellation.isDisposedAsync) {
    ///             // Object is disposed asynchronously, release resources asynchronously  
    ///         }
    ///     }   
    /// }
    /// </example>
    public interface IExposedAsyncCancellation: IExposedDisposedSync, IExposedDisposedAsync
    {
        public enum CancellationType
        {
            None,
            Sync,
            Async
        }
        public Signal cancellationSignal { get; }
        public CancellationType cancellationType => isDisposedSync ? CancellationType.Sync : (isDisposedAsync ? CancellationType.Async : CancellationType.None);
    }
}