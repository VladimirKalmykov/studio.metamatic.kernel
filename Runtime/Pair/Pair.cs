using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic
{
    public class Pair<TVendor, TClient> : MicroBehaviourLegacy
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

        protected override async UniTask Main(CancellationSignal signal)
        {
            using var clientProviderListener = m_ClientProvider.OnPairingRequest(OnAnnounceClient);
            await signal;
        }
        
        private async UniTaskVoid OnAnnounceVendor(TVendor vendor, TClient client, CancellationToken vendorCancellationToken, CancellationToken clientCancellationToken)
        {
            var combinedCancellationTokenForVendor = CancellationTokenSource
                .CreateLinkedTokenSource(clientCancellationToken, disposeSignal).Token;
            var combinedCancellationTokenForClient = CancellationTokenSource
                .CreateLinkedTokenSource(vendorCancellationToken, disposeSignal).Token;
            
            /*
             * To client OnPaired methods passes cancellation token that combined from 1. client cancellation token, 2. vendor cancellation token, 3. pair dispose cancellation token.
             * Thus, onPaired for client will be cancelled if client is disposed or vendor is disposed or pair is disposed.
             */
            var clientPairedTask = client.OnPaired(vendor, combinedCancellationTokenForClient);
            /*
             * To vendor OnPaired methods passes cancellation token that combined from 1. client cancellation token, 2. pair dispose cancellation token.
             * Thus, onPaired for vendor will be cancelled if client is disposed or pair is disposed.
             */
            var vendorPairedTask = vendor.OnPaired(client, combinedCancellationTokenForVendor);
            try
            {
               
                await UniTask.WhenAll(clientPairedTask, vendorPairedTask);
            }
            catch (OperationCanceledException)
            {
                // Ignore
            }
            catch (System.Exception e)
            {
                Debug.LogError(e);
            }
        }

        private void OnAnnounceClient(TClient client, CancellationToken clientCancellationToken)
        {
            var combinedToken = CancellationTokenSource
                .CreateLinkedTokenSource(clientCancellationToken, disposeSignal).Token;
            m_VendorProvider.OnPairingRequest(client, (vendor, vendorCancellationToken) =>
            {
                OnAnnounceVendor(vendor, client, vendorCancellationToken, combinedToken).Forget();
            }, combinedToken);
        }
    }
}