using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Metamatic
{
    public class DuplexApiWireProxySource<T>: IDisposable
    where T: class
    {
        public readonly ObservableProxy<T> Proxy = new();
        public event Action OnDisposed;

        public DuplexApiWireProxySource(ObservableReadOnly<T> state)
        {
            SetProxy(state);
        }

        public void SetProxy(ObservableReadOnly<T> state)
        {
            Proxy.SetProxy(state);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DisposableAction Subscribe(Action<T> action)
        {
            return Proxy.Watch(action);
        }
        
        public DisposableAction SubscribeDisposed(Action action)
        {
            OnDisposed += action;
            return new DisposableAction(() => OnDisposed -= action);
        }

        public void Dispose()
        {
            Proxy?.Dispose();
            OnDisposed?.Invoke();
        }
    }
    public class DuplexApiWireVendor<TVendor, TClient>: OutputModernWire<DuplexApiWireClient<TClient, TVendor>>, IDuplexApiWireVendor<TVendor, TClient>
        where TVendor: class
        where TClient: class
    {
        public DuplexApiWireVendor(TVendor vendor, IDuplexApiWireVendor<TVendor, TClient>.ClientSupply supply) : base()
        {
            Set(vendor);
            OnDispose += Subscribe(supply);
        }
        public DuplexApiWireVendor(IDuplexApiWireVendor<TVendor, TClient>.ClientSupply supply) : base()
        {
            OnDispose += Subscribe(supply);
        }
        public DuplexApiWireVendor(TVendor vendor) : base()
        {
            Set(vendor);
        }
        public DuplexApiWireVendor() : base()
        {
        }
        private Observable<TVendor> m_VendorState = new();
        private Dictionary<IModernWire, DuplexApiWireProxySource<TClient>> m_ClientStateProxies = new();
        private event Action<DuplexApiWireProxySource<TClient>> OnClientProxyInitialized;
        
        protected override void OnInputWireConnected(DuplexApiWireClient<TClient, TVendor> inputWire)
        {
            ((IDuplexApiWireClient<TClient, TVendor>)inputWire).InternalSetVendor(this, m_VendorState);
        }

        protected override void OnInputWireDisconnected(DuplexApiWireClient<TClient, TVendor> inputWire)
        {
            ((IDuplexApiWireClient<TClient, TVendor>)inputWire).InternalSetVendor(this, null);
        }

        public TClient[] clients => m_ClientStateProxies.Values.Select(p => p.Proxy.value).ToArray();
        public int clientsCount => m_ClientStateProxies.Count;

        public DisposableAction Subscribe(IDuplexApiWireVendor<TVendor, TClient>.ClientSupply supply)
        {

            List<Action> disposables = new List<Action>();
            void ProxySourceHandler(DuplexApiWireProxySource<TClient> proxySource)
            {
                var cts = new CancellationTokenSource();
                void ClientHandler(TClient client)
                {
                    cts?.Cancel();
                    if (client == null) return;
                    cts = new CancellationTokenSource();
                    supply(client, cts.Token);
                }
                var subscribeValue = proxySource.Subscribe(ClientHandler);
                Action subscribeProxyDisposed = default;
                subscribeProxyDisposed = proxySource.SubscribeDisposed(() =>
                {
                    subscribeValue.Dispose();
                    disposables.Remove(subscribeValue);
                    disposables.Remove(subscribeProxyDisposed);
                });
                disposables.Add(subscribeProxyDisposed);
                disposables.Add(subscribeValue);
            }
            
            OnClientProxyInitialized += ProxySourceHandler;
            
            return new DisposableAction(() =>
            {
                OnClientProxyInitialized -= ProxySourceHandler;
                disposables.ForEach(d => d());
            });
        }

        void IDuplexApiWireVendor<TVendor, TClient>.InternalSetClient(IDuplexApiWireClient<TClient, TVendor> wire, ObservableReadOnly<TClient> clientState)
        {
            if (m_ClientStateProxies.TryGetValue(wire, out var proxySource))
            {
                proxySource.SetProxy(clientState);
                return;
            }

            proxySource = new DuplexApiWireProxySource<TClient>(clientState);
            m_ClientStateProxies.Add(wire, proxySource);
            OnClientProxyInitialized?.Invoke(proxySource);
        }

        void IDuplexApiWireVendor<TVendor, TClient>.InternalRemoveClient(IDuplexApiWireClient<TClient, TVendor> wire)
        {
            if (m_ClientStateProxies.Remove(wire, out var proxySource))
            {
                proxySource.Dispose();
            }
        }

        public void Set(TVendor vendor)
        {
            m_VendorState.value = vendor;
        }

        public bool TryGetClient(out TClient client)
        {
            if (m_ClientStateProxies.Values.FirstOrDefault(proxy => proxy.Proxy.value != null) is { } proxy)
            {
                client = proxy.Proxy.value;
                return true;
            }
            client = default;
            return false;
        }
    }
    
    public class DuplexApiWireClient<TClient, TVendor>: InputModernWire<DuplexApiWireVendor<TVendor, TClient>>, IDuplexApiWireClient<TClient, TVendor>
        where TVendor: class
        where TClient: class
    {
        public DuplexApiWireClient(TClient client, IDuplexApiWireClient<TClient, TVendor>.VendorSupply supply) : base(IModernWire.ConnectionType.SingleError)
        {
            Set(client);
            OnDispose += Subscribe(supply);
        }
        public DuplexApiWireClient(IDuplexApiWireClient<TClient, TVendor>.VendorSupply supply) : base(IModernWire.ConnectionType.SingleError)
        {
            OnDispose += Subscribe(supply);
        }
        public DuplexApiWireClient(TClient client) : base(IModernWire.ConnectionType.SingleError)
        {
            Set(client);
        }
        public DuplexApiWireClient() : base()
        {
        }
        private Observable<TClient> m_ClientState = new();
        private Dictionary<IModernWire, DuplexApiWireProxySource<TVendor>> m_VendorStateProxies = new();
        private event Action<DuplexApiWireProxySource<TVendor>> OnVendorProxyInitialized;
        protected override void OnOutputWireConnected(DuplexApiWireVendor<TVendor, TClient> outputWire)
        {
            ((IDuplexApiWireVendor<TVendor, TClient>)outputWire).InternalSetClient(this, m_ClientState);
        }

        protected override void OnOutputWireDisconnected(DuplexApiWireVendor<TVendor, TClient> outputWire)
        {
            ((IDuplexApiWireVendor<TVendor, TClient>)outputWire).InternalSetClient(this, null);
        }

        public TVendor[] vendors => m_VendorStateProxies.Values.Select(p => p.Proxy.value).ToArray();
        public int vendorsCount => m_VendorStateProxies.Count;

        public DisposableAction Subscribe(IDuplexApiWireClient<TClient, TVendor>.VendorSupply supply)
        {
            List<Action> disposables = new List<Action>();
            void ProxySourceHandler(DuplexApiWireProxySource<TVendor> proxySource)
            {
                var cts = new CancellationTokenSource();
                void VendorHandler(TVendor vendor)
                {
                    cts?.Cancel();
                    if (vendor == null) return;
                    cts = new CancellationTokenSource();
                    supply(vendor, cts.Token);
                }
                var subscribeValue = proxySource.Subscribe(VendorHandler);
                Action subscribeProxyDisposed = default;
                subscribeProxyDisposed = proxySource.SubscribeDisposed(() =>
                {
                    subscribeValue.Dispose();
                    disposables.Remove(subscribeValue);
                    disposables.Remove(subscribeProxyDisposed);
                });
                disposables.Add(subscribeProxyDisposed);
                disposables.Add(subscribeValue);
            }
            
            OnVendorProxyInitialized += ProxySourceHandler;
            
            /* Handle already initialized proxies */
            foreach (var proxy in m_VendorStateProxies.Values)
            {
                ProxySourceHandler(proxy);
            }
            
            return new DisposableAction(() =>
            {
                OnVendorProxyInitialized -= ProxySourceHandler;
                disposables.ForEach(d => d());
            });
        }

        void IDuplexApiWireClient<TClient, TVendor>.InternalSetVendor(IDuplexApiWireVendor<TVendor, TClient> wire, ObservableReadOnly<TVendor> vendorState)
        {
            if (m_VendorStateProxies.TryGetValue(wire, out var proxySource))
            {
                proxySource.SetProxy(vendorState);
                return;
            }

            proxySource = new DuplexApiWireProxySource<TVendor>(vendorState);
            m_VendorStateProxies.Add(wire, proxySource);
            OnVendorProxyInitialized?.Invoke(proxySource);
        }

        void IDuplexApiWireClient<TClient, TVendor>.InternalRemoveVendor(IDuplexApiWireVendor<TVendor, TClient> wire)
        {
            if (m_VendorStateProxies.Remove(wire, out var proxySource))
            {
                proxySource.Dispose();
            }
        }

        public void Set(TClient client)
        {
            m_ClientState.value = client;
        }

        public bool TryGetVendor(out TVendor vendor)
        {
            if (m_VendorStateProxies.Values.FirstOrDefault(proxy => proxy.Proxy.value != null) is { } proxy)
            {
                vendor = proxy.Proxy.value;
                return true;
            }
            vendor = default;
            return false;
        }
    }
}