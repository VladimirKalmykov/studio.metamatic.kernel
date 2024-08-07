using System.Threading;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    public class OneWayPair<TVendor, TClient>: MicroBehaviour
    where TVendor: class
    where TClient: class
    {
        public static OneWayPair<TVendor, TClient> Create(IOneWayPairVendorProvider<TVendor, TClient> vendorProvider, IOneWayPairClientProvider<TClient, TVendor> clientProvider)
        {
            return new OneWayPair<TVendor, TClient>(vendorProvider, clientProvider);
        }
        private readonly IOneWayPairClientProvider<TClient, TVendor> m_ClientProvider;
        private readonly IOneWayPairVendorProvider<TVendor, TClient> m_VendorProvider;

        private OneWayPair(IOneWayPairVendorProvider<TVendor, TClient> vendorProvider, IOneWayPairClientProvider<TClient, TVendor> clientProvider)
        {
            m_VendorProvider = vendorProvider;
            m_ClientProvider = clientProvider;
            Run();
        }

        protected override async UniTask Main(CancellationToken token)
        {
            using var clientProviderListener = m_ClientProvider.OnPairingRequest(OnAnnounceClient);
            await token;
        }
        
        private void OnAnnounceClient(TClient client, CancellationToken clientCancellationToken)
        {
            var combinedToken = CancellationTokenSource.CreateLinkedTokenSource(clientCancellationToken, disposeCancellationToken).Token;
            m_VendorProvider.OnPairingRequest(client, combinedToken);
        }
    }
}