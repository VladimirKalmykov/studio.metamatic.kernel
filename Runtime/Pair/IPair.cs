using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    public interface IPairVendorProvider<out TVendor, in TClient>
        where TVendor: class, IPairVendor<TClient>
        where TClient: class, IPairClient<TVendor>
    {
        public UniTaskVoid OnPairingRequest(TClient client, Action<TVendor, CancellationToken> announceVendor, CancellationToken cancellationToken);
    }
    
    public interface IPairClientProvider<out TClient, TVendor>
        where TVendor: class, IPairVendor<TClient>
        where TClient: class, IPairClient<TVendor>
    {
        public DisposableAction OnPairingRequest(Action<TClient, CancellationToken> announceClient);
    }
    
    public interface IPairVendor<in TClient>
        where TClient: class
    {
        public UniTask OnPaired(TClient client, CancellationToken clientCancellationToken);
    }

    public interface IPairClient<in TVendor>
        where TVendor: class
    {
        public UniTask OnPaired(TVendor vendor, CancellationToken vendorCancellationToken);
    }
}