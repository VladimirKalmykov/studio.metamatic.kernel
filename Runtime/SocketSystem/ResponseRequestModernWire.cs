using Metamatic;

namespace Metamatic
{
    public class RequesterModernWire<TRequest, TResponse>: OutputModernWire<IResponderModernWire<TRequest, TResponse>>, IRequesterModernWire<TRequest, TResponse>
    {
        public IRequesterModernWire<TRequest, TResponse>.RequestHandler requestHandler { get; set; }
        public TResponse Request(TRequest request)
        {
            if (requestHandler != null)
            {
                return requestHandler(request);
            }
            return default;
        }

        protected override void OnInputWireConnected(IResponderModernWire<TRequest, TResponse> inputWire)
        {
            
        }

        protected override void OnInputWireDisconnected(IResponderModernWire<TRequest, TResponse> inputWire)
        {
            
        }
    }
    
    public class ResponderModernWire<TRequest, TResponse>: InputModernWire<IRequesterModernWire<TRequest, TResponse>>, IResponderModernWire<TRequest, TResponse>
    {
        private IRequesterModernWire<TRequest, TResponse>.RequestHandler m_RequestHandler = DefaultRequestHandler;
        protected override void OnOutputWireConnected(IRequesterModernWire<TRequest, TResponse> outputWire)
        {
            ((RequesterModernWire<TRequest, TResponse>)outputWire).requestHandler = m_RequestHandler;
        }

        protected override void OnOutputWireDisconnected(IRequesterModernWire<TRequest, TResponse> outputWire)
        {
            ((RequesterModernWire<TRequest, TResponse>)outputWire).requestHandler = null;
        }

        public DisposableAction Assign(IRequesterModernWire<TRequest, TResponse>.RequestHandler handler)
        {
            m_RequestHandler = handler;
            return new DisposableAction(() =>
            {
                m_RequestHandler = DefaultRequestHandler;
            });
        }
        
        private static TResponse DefaultRequestHandler(TRequest request)
        {
            throw new System.NotImplementedException();
        }
    }
}