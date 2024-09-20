namespace Metamatic
{
    public class ApiModernWireClient<TApi>: OutputModernWire<ApiModernWireImplement<TApi>>, IApiModernWireClient<TApi>
        where TApi: class, new()
    {
        private ObservableReadOnly<TApi> m_ProxyApi;
        private readonly TApi m_FallbackApi = new();

        public TApi Api
        {
            get
            {
                if (m_ProxyApi != null && m_ProxyApi.value != null)
                {
                    return m_ProxyApi.value;
                }

                return m_FallbackApi;
            }
        }

        void IApiModernWireClient<TApi>.InternalSetApi(ObservableReadOnly<TApi> apiState)
        {
            m_ProxyApi = apiState;
        }

        void IApiModernWireClient<TApi>.InternalWithdrawApi(ObservableReadOnly<TApi> apiState)
        {
            if (m_ProxyApi == apiState)
            {
                m_ProxyApi = null;
            }
        }

        protected override void OnInputWireConnected(ApiModernWireImplement<TApi> inputWire)
        {
            
        }

        protected override void OnInputWireDisconnected(ApiModernWireImplement<TApi> inputWire)
        {
            
        }
    }
    
    public class ApiModernWireImplement<TApi>: InputModernWire<ApiModernWireClient<TApi>>, IApiModernWireImplement<TApi>
        where TApi: class, new()
    {
        private readonly Observable<TApi> m_ApiState = new();
        public void ImplementApi(TApi api)
        {
            m_ApiState.value = api;
        }

        protected override void OnOutputWireConnected(ApiModernWireClient<TApi> outputWire)
        {
            ((IApiModernWireClient<TApi>)outputWire).InternalSetApi(m_ApiState);
        }

        protected override void OnOutputWireDisconnected(ApiModernWireClient<TApi> outputWire)
        {
            ((IApiModernWireClient<TApi>)outputWire).InternalWithdrawApi(m_ApiState);
        }
    }
}