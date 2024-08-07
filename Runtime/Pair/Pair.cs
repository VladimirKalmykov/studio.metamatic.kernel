using System.Threading;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    public class Pair<TVendor, TClient> : MicroBehaviour
        where TVendor : class, IPairVendor<TClient>
        where TClient : class, IPairClient<TVendor>
    {
        public static Pair<TVendor, TClient> Create(IPairVendorProvider<TVendor, TClient> vendorProvider,
            IPairClientProvider<TClient, TVendor> clientProvider)
        {
            return new Pair<TVendor, TClient>(vendorProvider, clientProvider);
        }

        private readonly IPairClientProvider<TClient, TVendor> m_ClientProvider;
        private readonly IPairVendorProvider<TVendor, TClient> m_VendorProvider;

        private Pair(IPairVendorProvider<TVendor, TClient> vendorProvider,
            IPairClientProvider<TClient, TVendor> clientProvider)
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
            var combinedToken = CancellationTokenSource
                .CreateLinkedTokenSource(clientCancellationToken, disposeCancellationToken).Token;
            m_VendorProvider.OnPairingRequest(client, (vendor, vendorCancellationToken) =>
            {
                var combinedPlusVendorToken = CancellationTokenSource
                    .CreateLinkedTokenSource(vendorCancellationToken, disposeCancellationToken).Token;
                /*
                 * To client OnPaired methods passes cancellation token that combined from 1. client cancellation token, 2. vendor cancellation token, 3. pair dispose cancellation token.
                 * Thus, onPaired for client will be cancelled if client is disposed or vendor is disposed or pair is disposed.
                 */
                client.OnPaired(vendor, combinedPlusVendorToken);
                /*
                 * To vendor OnPaired methods passes cancellation token that combined from 1. client cancellation token, 2. pair dispose cancellation token.
                 * Thus, onPaired for vendor will be cancelled if client is disposed or pair is disposed.
                 */
                vendor.OnPaired(client, combinedToken);
            }, combinedToken);
        }
    }
}