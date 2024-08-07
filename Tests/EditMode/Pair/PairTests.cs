using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Metamatic;
using NUnit.Framework;

namespace Tests.EditMode.Pair
{
    public class MockClient : IPairClient<MockVendor>, IDisposable
    {
        public readonly CancellationTokenSource disposeCancellationSource = new CancellationTokenSource();
        public MockVendor pairedVendor { get; private set; }
        public void OnPaired(MockVendor vendor, CancellationToken cancellationToken)
        {
            pairedVendor = vendor;
            cancellationToken.Register(() => pairedVendor = null);
        }

        public void Dispose()
        {
            disposeCancellationSource.Cancel();
        }
    }
    public class MockVendor : IPairVendor<MockClient>, IDisposable
    {
        public readonly CancellationTokenSource disposeCancellationSource = new CancellationTokenSource();
        public readonly MockClient client;
        public readonly List<MockClient> clients = new List<MockClient>();
        public MockClient pairedClient { get; private set; }
        public MockVendor(MockClient client)
        {
            this.client = client;
        }
        public void OnPaired(MockClient client, CancellationToken cancellationToken)
        {
            pairedClient = client;
            clients.Add(client);
            cancellationToken.Register(() =>
            {
                pairedClient = null;
                clients.Remove(client);
            });
        }

        public void Dispose()
        {
            disposeCancellationSource.Cancel();
        }
    }
    public class Provider : IPairVendorProvider<MockVendor, MockClient>, IPairClientProvider<MockClient, MockVendor>
    {
        public MockVendor currentVendor { get; private set; }
        public MockClient currentClient { get; private set; }
        public readonly List<MockVendor> Vendors = new List<MockVendor>();
        private event Action<MockClient> OnClientAnnounce;
        public async UniTaskVoid OnPairingRequest(MockClient client, Action<MockVendor, CancellationToken> announceVendor, CancellationToken cancellationToken)
        {
            var vendor = new MockVendor(client);
            Vendors.Add(vendor);
            currentVendor = vendor;
            announceVendor(vendor, vendor.disposeCancellationSource.Token);
            await cancellationToken;
            Vendors.Remove(vendor);
            vendor.Dispose();
        }

        public DisposableAction OnPairingRequest(Action<MockClient, CancellationToken> announceClient)
        {
            var disposeClientCancellationTokenSource = new CancellationTokenSource();
            var clientHandler = new Action<MockClient>((client) =>
            {
                announceClient(client,
                    CancellationTokenSource.CreateLinkedTokenSource(client.disposeCancellationSource.Token,
                        disposeClientCancellationTokenSource.Token).Token);
            });
            OnClientAnnounce += clientHandler;
            return new DisposableAction(disposeClientCancellationTokenSource.Cancel);
        }

        public MockClient CreateClient()
        {
            var client = new MockClient();
            currentClient = client;
            OnClientAnnounce?.Invoke(client);
            return client;
        }
    }
    [TestFixture]
    public class PairTests
    {
        [Test]
        public void TestPairing()
        {
            var provider = new Provider();
            var pair = Pair<MockVendor, MockClient>.Create(provider, provider);
            
            provider.CreateClient();
            
            Assert.AreEqual(provider.currentVendor, provider.currentClient.pairedVendor);
            Assert.AreEqual(provider.currentClient, provider.currentVendor.pairedClient);
        }
        
        [Test]
        public void TestPairing_DisposeVendor()
        {
            var provider = new Provider();
            var pair = Pair<MockVendor, MockClient>.Create(provider, provider);
            
            provider.CreateClient();
            
            Assert.AreEqual(provider.currentVendor, provider.currentClient.pairedVendor);
            Assert.AreEqual(provider.currentClient, provider.currentVendor.pairedClient);
            
            provider.currentVendor.Dispose();
            Assert.IsNull(provider.currentClient.pairedVendor);
        }
        
        [Test]
        public void TestPairing_DisposeClient()
        {
            var provider = new Provider();
            var pair = Pair<MockVendor, MockClient>.Create(provider, provider);
            
            provider.CreateClient();
            
            Assert.AreEqual(provider.currentVendor, provider.currentClient.pairedVendor);
            Assert.AreEqual(provider.currentClient, provider.currentVendor.pairedClient);
            
            provider.currentClient.Dispose();
            Assert.IsNull(provider.currentClient.pairedVendor);
            Assert.IsNull(provider.currentVendor.pairedClient);
        }

        [Test]
        public void TestPairing_DisposePair()
        {
            var provider = new Provider();
            var pair = Pair<MockVendor, MockClient>.Create(provider, provider);
            
            provider.CreateClient();
            
            Assert.AreEqual(provider.currentVendor, provider.currentClient.pairedVendor);
            Assert.AreEqual(provider.currentClient, provider.currentVendor.pairedClient);
            
            pair.Dispose();
            Assert.IsNull(provider.currentClient.pairedVendor);
            Assert.IsNull(provider.currentVendor.pairedClient);
        }

        [Test]
        public void TestPairingTwoClients()
        {
            var provider = new Provider();
            var pair = Pair<MockVendor, MockClient>.Create(provider, provider);
            
            var client1 = provider.CreateClient();
            var client2 = provider.CreateClient();
            
            Assert.AreEqual(2, provider.Vendors.Count);
            Assert.AreEqual(provider.Vendors[0].pairedClient, client1);
            Assert.AreEqual(provider.Vendors[1].pairedClient, client2);
            
            /* Dispose first client */
            client1.Dispose();
            
            Assert.AreEqual(1, provider.Vendors.Count);
            
            /* Dispose second client */
            client2.Dispose();
            
            Assert.AreEqual(0, provider.Vendors.Count);
        }
    }
}