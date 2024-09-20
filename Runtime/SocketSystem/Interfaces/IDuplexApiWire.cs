using System.Threading;

namespace Metamatic
{
    public interface IDuplexApiWireVendor<TVendor, TClient>: IOutputModernWire
        where TVendor: class
        where TClient: class
    {
        public delegate void ClientSupply(TClient client, CancellationToken cancellationToken);
        public TClient[] clients { get; }
        public int clientsCount { get; }
        public DisposableAction Subscribe(ClientSupply supply);
        internal void InternalSetClient(IDuplexApiWireClient<TClient, TVendor> wire, ObservableReadOnly<TClient> clientState);
        internal void InternalRemoveClient(IDuplexApiWireClient<TClient, TVendor> wire);
        public void Set(TVendor vendor);
        /// <summary>
        /// If the client is available, it will be returned in the out parameter and the method will return true. Otherwise, it will return false.
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public bool TryGetClient(out TClient client);
    }
    
    public interface IDuplexApiWireClient<TClient, TVendor>: IInputModernWire
        where TVendor: class
        where TClient: class
    {
        public delegate void VendorSupply(TVendor vendor, CancellationToken cancellationToken);
        public TVendor[] vendors { get; }
        public int vendorsCount { get; }
        public DisposableAction Subscribe(VendorSupply supply);
        internal void InternalSetVendor(IDuplexApiWireVendor<TVendor, TClient> wire, ObservableReadOnly<TVendor> vendorState);
        internal void InternalRemoveVendor(IDuplexApiWireVendor<TVendor, TClient> wire);
        public void Set(TClient client);
        /// <summary>
        /// If the vendor is available, it will be returned in the out parameter and the method will return true. Otherwise, it will return false.
        /// </summary>
        /// <param name="vendor"></param>
        /// <returns></returns>
        public bool TryGetVendor(out TVendor vendor);
    }
}