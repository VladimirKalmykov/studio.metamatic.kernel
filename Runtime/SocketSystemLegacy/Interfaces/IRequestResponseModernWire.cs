namespace Metamatic
{
    public interface IRequesterModernWire<TRequest, TResponse> : IOutputModernWire<IResponderModernWire<TRequest, TResponse>>
    {
        public delegate TResponse RequestHandler(TRequest request);
        public TResponse Request(TRequest request);
    }
    
    public interface IResponderModernWire<TRequest, TResponse> : IInputModernWire<IRequesterModernWire<TRequest, TResponse>>
    {
        public DisposableAction Assign(IRequesterModernWire<TRequest, TResponse>.RequestHandler handler);
    }
}