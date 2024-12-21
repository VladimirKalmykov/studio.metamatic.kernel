using Cysharp.Threading.Tasks;

namespace Metamatic
{
    /// <summary>
    /// Describes the mechanism of providing a placement. If some object needs to be placed in the world, it should use the placement provider.
    /// This provider reserves the right to decide where and how to place the object. For example, you have health indicator that should be placed above the character.
    /// The character can provide object that implements IPlacementProvider, which reserves the place above the character and provide positioning for the health indicator.
    /// As long as each character can have different height, the health indicator should be placed above the character, not at the fixed position.
    /// So, the character provides the placement provider as a universal way to place the health indicator.
    /// </summary>
    public interface IPlacementProvider
    {
        /// <summary>
        /// Placement request handler. This handler is called when the placement is ready to use. Positioning supply providers coords and anchor for the placement.
        /// Exposed async cancellation is used to track the disposal of the placement. Placement can be disposed synchronously or asynchronously as from IPolyDisposableOperation,
        /// also from IPlacementProvider itself (e.g. if the placement source object is removed from the scene or hidden).
        /// PlacementRequestHandler can be called multiple times, but each time its operation should be disposed before the next call.
        /// If operation is disposed asynchronously, the IPlacementProvider should wait for handler to finish its work.
        /// If operation is disposed synchronously, the IPlacementProvider still should wait for handler to finish its work, but handle should take into account that the operation is disposed synchronously,
        /// and should release resources immediately.
        /// </summary>
        public delegate UniTask PlacementRequestHandler(IPositioningSupply positioningSupply, IExposedAsyncCancellation operation);
        /// <summary>
        /// Request placement. This method should be called by the object that needs to be placed. When the placement is ready, the handler is called.
        /// During the placement, handler can be called multiple times, but each time its operation should be disposed before the next call.
        /// You can track the disposal of the placement by the operation.cancellationSignal and operation.cancellationType properties.
        /// If cancellationType is Sync, you should release resources immediately. If cancellationType is Async, you should release resources asynchronously (e.g. by fade out).
        /// The request returns IPolyDisposableOperation that can be used to dispose the placement request also asynchronously or synchronously.
        /// Dispose it asynchronously if you need to wait for the placement to finish its work. Dispose it synchronously if you need to release resources immediately (e.g. if the object is removed from the scene).
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        public IPolyDisposableOperation RequestPlacement(PlacementRequestHandler handler);
    }
}