using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    public interface IOneWayPairVendorProvider<TVendor, in TClient>
    where TVendor: class
    where TClient: class
    {
        public UniTaskVoid OnPairingRequest(TClient client, CancellationToken cancellationToken);
    }
    
    public interface IOneWayPairClientProvider<out TClient, TVendor>
    where TVendor: class
    where TClient: class
    {
        public DisposableAction OnPairingRequest(Action<TClient, CancellationToken> announceClient);
    }
}