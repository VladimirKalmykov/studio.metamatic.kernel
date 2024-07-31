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
        public DisposableAction SubscribeClient(ClientSupply supply);
        internal void InternalSetClient(IDuplexApiWireClient<TClient, TVendor> wire, MicroStateReadOnly<TClient> clientState);
        internal void InternalRemoveClient(IDuplexApiWireClient<TClient, TVendor> wire);
        public void SetVendor(TVendor vendor);
    }
    
    public interface IDuplexApiWireClient<TClient, TVendor>: IInputModernWire
        where TVendor: class
        where TClient: class
    {
        public delegate void VendorSupply(TVendor vendor, CancellationToken cancellationToken);
        public TVendor[] vendors { get; }
        public int vendorsCount { get; }
        public DisposableAction SubscribeVendor(VendorSupply supply);
        internal void InternalSetVendor(IDuplexApiWireVendor<TVendor, TClient> wire, MicroStateReadOnly<TVendor> vendorState);
        internal void InternalRemoveVendor(IDuplexApiWireVendor<TVendor, TClient> wire);
        public void SetClient(TClient client);
    }
}