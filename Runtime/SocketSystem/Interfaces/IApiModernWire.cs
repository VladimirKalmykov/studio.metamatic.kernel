namespace Metamatic
{
    public interface IApiModernWireClient<TApi>: IOutputModernWire
        where TApi: class, new()
    {
        // ReSharper disable once InconsistentNaming
        public TApi Api { get; }
        
        internal void InternalSetApi(MicroStateReadOnly<TApi> apiState);
        internal void InternalWithdrawApi(MicroStateReadOnly<TApi> apiState);
    }
    
    public interface IApiModernWireImplement<in TApi>: IInputModernWire
        where TApi: class, new()
    {
        public void ImplementApi(TApi api);
    }
}