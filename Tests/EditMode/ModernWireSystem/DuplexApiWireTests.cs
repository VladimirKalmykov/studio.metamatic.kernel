using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Metamatic;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Tests.EditMode.ModernWireSystem
{
    [TestFixture]
    public class DuplexApiWireTests
    {
        interface IVendorApi
        {
            string Foo();
        }
        
        interface IClientApi
        {
            string Bar();
        }
        
        class VendorApi: IVendorApi
        {
            public virtual string Foo()
            {
                return "Foo";
            }
        }
        
        class ClinetApi: IClientApi
        {
            public string Bar()
            {
                return "Bar";
            }
        }
        
        class SpecialVendorApi : VendorApi
        {
            public override string Foo()
            {
                return "SpecialFoo";
            }
        }
        
        [Test]
        public void ConnectionTest()
        {
            var vendorApi = new VendorApi();
            var clientApi = new ClinetApi();
            
            var vendor = new DuplexApiWireVendor<VendorApi, ClinetApi>();
            vendor.Set(vendorApi);
            
            var client = new DuplexApiWireClient<ClinetApi, VendorApi>();
            client.Set(clientApi);
           
            vendor.ConnectOut(client);
            
            Assert.AreEqual(1, vendor.clientsCount);
            Assert.AreEqual(1, client.vendorsCount);
            Assert.AreEqual("Bar", vendor.clients[0].Bar());
            Assert.AreEqual("Foo", client.vendors[0].Foo());
        }
        
        [UnityTest]
        public IEnumerator AsyncConnectionTest() => UniTask.ToCoroutine(async () =>
        {
            var vendorApi = new VendorApi();
            var clientApi = new ClinetApi();
            
            var vendor = new DuplexApiWireVendor<VendorApi, ClinetApi>();
            vendor.Set(vendorApi);
            
            var client = new DuplexApiWireClient<ClinetApi, VendorApi>();
            
            client.Set(clientApi);

            var clientHistory = new List<ClinetApi>();
            var vendorHistory = new List<VendorApi>();
            var clientTokenHistory = new List<CancellationToken>();
            var vendorTokenHistory = new List<CancellationToken>();
            
            vendor.Subscribe((api, token) =>
            {
                clientHistory.Add(api);
                clientTokenHistory.Add(token);
            });
            
            client.Subscribe((api, token) =>
            {
                vendorHistory.Add(api);
                vendorTokenHistory.Add(token);
            });
            
            vendor.ConnectOut(client);
            
            await UniTask.Delay(100);
            
            Assert.AreEqual(1, clientHistory.Count);
            Assert.AreEqual(1, vendorHistory.Count);
            
            Assert.IsFalse(clientTokenHistory[0].IsCancellationRequested);
            Assert.IsFalse(vendorTokenHistory[0].IsCancellationRequested);
            
            Assert.AreEqual(vendorApi, vendorHistory[0]);
            Assert.AreEqual(clientApi, clientHistory[0]);
        });
        
        [UnityTest]
        public IEnumerator PostConnectionSetInstancesTest() => UniTask.ToCoroutine(async () =>
        {
            var vendorApi = new VendorApi();
            var clientApi = new ClinetApi();
            
            var vendor = new DuplexApiWireVendor<VendorApi, ClinetApi>();
            
            var client = new DuplexApiWireClient<ClinetApi, VendorApi>();

            var clientHistory = new List<ClinetApi>();
            var vendorHistory = new List<VendorApi>();
            var clientTokenHistory = new List<CancellationToken>();
            var vendorTokenHistory = new List<CancellationToken>();
            
            vendor.Subscribe((api, token) =>
            {
                clientHistory.Add(api);
                clientTokenHistory.Add(token);
            });
            
            client.Subscribe((api, token) =>
            {
                vendorHistory.Add(api);
                vendorTokenHistory.Add(token);
            });
            
            vendor.ConnectOut(client);
            client.Set(clientApi);
            
            vendor.Set(vendorApi);
            
            await UniTask.Delay(100);
            
            Assert.AreEqual(1, clientHistory.Count);
            Assert.AreEqual(1, vendorHistory.Count);
            
            Assert.IsFalse(clientTokenHistory[0].IsCancellationRequested);
            Assert.IsFalse(vendorTokenHistory[0].IsCancellationRequested);
            
            Assert.AreEqual(vendorApi, vendorHistory[0]);
            Assert.AreEqual(clientApi, clientHistory[0]);
        });
        
        [UnityTest]
        public IEnumerator DisconnectInstancesTest() => UniTask.ToCoroutine(async () =>
        {
            var vendorApi = new VendorApi();
            var clientApi = new ClinetApi();
            
            var vendor = new DuplexApiWireVendor<VendorApi, ClinetApi>();
            
            var client = new DuplexApiWireClient<ClinetApi, VendorApi>();

            var clientHistory = new List<ClinetApi>();
            var vendorHistory = new List<VendorApi>();
            var clientTokenHistory = new List<CancellationToken>();
            var vendorTokenHistory = new List<CancellationToken>();
            
            vendor.Subscribe((api, token) =>
            {
                clientHistory.Add(api);
                clientTokenHistory.Add(token);
            });
            
            client.Subscribe((api, token) =>
            {
                vendorHistory.Add(api);
                vendorTokenHistory.Add(token);
            });
            
            vendor.ConnectOut(client);
            
            client.Set(clientApi);
            vendor.Set(vendorApi);
            
            await UniTask.Delay(50);
            
            client.Set(null);
            vendor.Set(null);
            
            Assert.AreEqual(1, clientHistory.Count);
            Assert.AreEqual(1, vendorHistory.Count);
            
            Assert.IsTrue(clientTokenHistory[0].IsCancellationRequested);
            Assert.IsTrue(vendorTokenHistory[0].IsCancellationRequested);
            
            Assert.AreEqual(vendorApi, vendorHistory[0]);
            Assert.AreEqual(clientApi, clientHistory[0]);
            
            client.Set(clientApi);
            vendor.Set(vendorApi);
            
            await UniTask.Delay(50);
            
            Assert.AreEqual(2, clientHistory.Count);
            
            Assert.IsFalse(clientTokenHistory[1].IsCancellationRequested);
            Assert.IsFalse(vendorTokenHistory[1].IsCancellationRequested);
            
            Assert.AreEqual(vendorApi, vendorHistory[1]);
            Assert.AreEqual(clientApi, clientHistory[1]);
        });
        
        [UnityTest]
        public IEnumerator MultipleClientsTest() => UniTask.ToCoroutine(async () =>
        {
            var vendorApi = new VendorApi();
            var clientApi1 = new ClinetApi();
            var clientApi2 = new ClinetApi();
            
            var vendor = new DuplexApiWireVendor<VendorApi, ClinetApi>();
            vendor.Set(vendorApi);
            
            var client1 = new DuplexApiWireClient<ClinetApi, VendorApi>();
            client1.Set(clientApi1);
            
            var client2 = new DuplexApiWireClient<ClinetApi, VendorApi>();
            client2.Set(clientApi2);
            
            vendor.ConnectOut(client1);
            vendor.ConnectOut(client2);
            
            Assert.AreEqual(2, vendor.clientsCount);
            Assert.AreEqual(1, client1.vendorsCount);
            Assert.AreEqual(1, client2.vendorsCount);
            
            Assert.AreEqual("Bar", vendor.clients[0].Bar());
            Assert.AreEqual("Bar", vendor.clients[1].Bar());
            
            Assert.AreEqual("Foo", client1.vendors[0].Foo());
            Assert.AreEqual("Foo", client2.vendors[0].Foo());
        });
        
        [UnityTest]
        public IEnumerator ChangeInstanceOfTheFlyTest() => UniTask.ToCoroutine(async () =>
        {
            var vendorApi = new VendorApi();
            var clientApi = new ClinetApi();
            
            var vendorTokenHistory = new List<CancellationToken>();
            var clientTokenHistory = new List<CancellationToken>();
            
            var vendor = new DuplexApiWireVendor<VendorApi, ClinetApi>();
            vendor.Set(vendorApi);
            
            var client = new DuplexApiWireClient<ClinetApi, VendorApi>();
            client.Set(clientApi);
            
            vendor.Subscribe((api, token) =>
            {
                clientTokenHistory.Add(token);
            });
            
            client.Subscribe((api, token) =>
            {
                vendorTokenHistory.Add(token);
            });
            
            vendor.ConnectOut(client);
            
            Assert.AreEqual(1, vendor.clientsCount);
            Assert.AreEqual(1, client.vendorsCount);
            
            Assert.AreEqual("Bar", vendor.clients[0].Bar());
            Assert.AreEqual("Foo", client.vendors[0].Foo());

            var specialVendorApi = new SpecialVendorApi();
            
            vendor.Set(specialVendorApi);
            
            await UniTask.Delay(50);
            
            Assert.AreEqual(1, vendor.clientsCount);
            Assert.AreEqual(1, client.vendorsCount);
            
            Assert.AreEqual("Bar", vendor.clients[0].Bar());
            Assert.AreEqual("SpecialFoo", client.vendors[0].Foo());
            
            Assert.IsTrue(vendorTokenHistory[0].IsCancellationRequested);
            Assert.IsFalse(vendorTokenHistory[1].IsCancellationRequested);
        });
        
        [UnityTest]
        public IEnumerator ChangeInstanceOfTheFlyWithMultipleClientsTest() => UniTask.ToCoroutine(async () =>
        {
            var vendorApi = new VendorApi();
            var clientApi1 = new ClinetApi();
            var clientApi2 = new ClinetApi();
            
            var vendorTokenHistory = new List<CancellationToken>();
            var clientTokenHistory = new List<CancellationToken>();
            
            var vendor = new DuplexApiWireVendor<VendorApi, ClinetApi>();
            vendor.Set(vendorApi);
            
            var client1 = new DuplexApiWireClient<ClinetApi, VendorApi>();
            client1.Set(clientApi1);
            
            var client2 = new DuplexApiWireClient<ClinetApi, VendorApi>();
            client2.Set(clientApi2);
            
            vendor.ConnectOut(client1);
            vendor.ConnectOut(client2);
            
            Assert.AreEqual(2, vendor.clientsCount);
            Assert.AreEqual(1, client1.vendorsCount);
            Assert.AreEqual(1, client2.vendorsCount);
            
            Assert.AreEqual("Bar", vendor.clients[0].Bar());
            Assert.AreEqual("Bar", vendor.clients[1].Bar());
            
            Assert.AreEqual("Foo", client1.vendors[0].Foo());
            Assert.AreEqual("Foo", client2.vendors[0].Foo());
            
            var specialVendorApi = new SpecialVendorApi();
            
            vendor.Set(specialVendorApi);
            
            await UniTask.Delay(50);
            
            Assert.AreEqual(2, vendor.clientsCount);
            Assert.AreEqual(1, client1.vendorsCount);
            Assert.AreEqual(1, client2.vendorsCount);
            
            Assert.AreEqual("Bar", vendor.clients[0].Bar());
            Assert.AreEqual("Bar", vendor.clients[1].Bar());
            
            Assert.AreEqual("SpecialFoo", client1.vendors[0].Foo());
            Assert.AreEqual("SpecialFoo", client2.vendors[0].Foo());
        });

        [UnityTest]
        public IEnumerator CancellationTokenMustBeCancelledOnSetNullValue() => UniTask.ToCoroutine(async () =>
        {
            var vendorApi = new VendorApi();
            var clientApi = new ClinetApi();

            var vendor = new DuplexApiWireVendor<VendorApi, ClinetApi>();
            vendor.Set(vendorApi);

            var client = new DuplexApiWireClient<ClinetApi, VendorApi>();
            client.Set(clientApi);

            var clientHistory = new List<ClinetApi>();
            var vendorHistory = new List<VendorApi>();
            var clientTokenHistory = new List<CancellationToken>();
            var vendorTokenHistory = new List<CancellationToken>();

            vendor.Subscribe((api, token) =>
            {
                clientHistory.Add(api);
                clientTokenHistory.Add(token);
            });

            client.Subscribe((api, token) =>
            {
                vendorHistory.Add(api);
                vendorTokenHistory.Add(token);
            });

            vendor.ConnectOut(client);
            client.Set(null);

            vendor.Set(null);

            await UniTask.Delay(100);

            Assert.AreEqual(1, clientHistory.Count);
            Assert.AreEqual(1, vendorHistory.Count);

            Assert.IsTrue(clientTokenHistory[0].IsCancellationRequested);
            Assert.IsTrue(vendorTokenHistory[0].IsCancellationRequested);

            Assert.AreEqual(vendorApi, vendorHistory[0]);
            Assert.AreEqual(clientApi, clientHistory[0]);
        });
        
        [Test]
        public void WorkWithInterfacesTest()
        {
            var vendorApi = new VendorApi();
            var clientApi = new ClinetApi();
            
            var vendor = new DuplexApiWireVendor<IVendorApi, IClientApi>();
            vendor.Set(vendorApi);
            
            var client = new DuplexApiWireClient<IClientApi, IVendorApi>();
            client.Set(clientApi);
           
            vendor.ConnectOut(client);
            
            Assert.AreEqual(1, vendor.clientsCount);
            Assert.AreEqual(1, client.vendorsCount);
            Assert.AreEqual("Bar", vendor.clients[0].Bar());
            Assert.AreEqual("Foo", client.vendors[0].Foo());
        }
    }
}