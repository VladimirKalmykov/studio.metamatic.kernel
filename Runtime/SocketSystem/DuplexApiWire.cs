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
        public readonly MicroStateProxy<T> Proxy = new();
        public event Action OnDisposed;

        public DuplexApiWireProxySource(MicroStateReadOnly<T> state)
        {
            SetProxy(state);
        }

        public void SetProxy(MicroStateReadOnly<T> state)
        {
            Proxy.SetProxy(state);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DisposableAction Subscribe(Action<T> action)
        {
            return Proxy.Subscribe(action);
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
        private MicroState<TVendor> m_VendorState = new();
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

        public DisposableAction SubscribeClient(IDuplexApiWireVendor<TVendor, TClient>.ClientSupply supply)
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

        void IDuplexApiWireVendor<TVendor, TClient>.InternalSetClient(IDuplexApiWireClient<TClient, TVendor> wire, MicroStateReadOnly<TClient> clientState)
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

        public void SetVendor(TVendor vendor)
        {
            m_VendorState.value = vendor;
        }
    }
    
    public class DuplexApiWireClient<TClient, TVendor>: InputModernWire<DuplexApiWireVendor<TVendor, TClient>>, IDuplexApiWireClient<TClient, TVendor>
        where TVendor: class
        where TClient: class
    {
        private MicroState<TClient> m_ClientState = new();
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

        public DisposableAction SubscribeVendor(IDuplexApiWireClient<TClient, TVendor>.VendorSupply supply)
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
            
            return new DisposableAction(() =>
            {
                OnVendorProxyInitialized -= ProxySourceHandler;
                disposables.ForEach(d => d());
            });
        }

        void IDuplexApiWireClient<TClient, TVendor>.InternalSetVendor(IDuplexApiWireVendor<TVendor, TClient> wire, MicroStateReadOnly<TVendor> vendorState)
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

        public void SetClient(TClient client)
        {
            m_ClientState.value = client;
        }
    }
}