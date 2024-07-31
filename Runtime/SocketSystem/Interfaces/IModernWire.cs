namespace Metamatic
{
    public interface IModernWire
    {
        public enum ConnectionType
        {
            /// <summary>
            /// This input can be connected to only one output, if another output is connected, the previous one will be disconnected
            /// </summary>
            Single,
            /// <summary>
            /// This input can be connected to only one output, if another output is connected an error will be thrown
            /// </summary>
            SingleError,
            /// <summary>
            /// This input can be connected to multiple outputs
            /// </summary>
            Multiple
        }
        public ConnectionType connectionType { get; }
        public bool Equals(IModernWire anotherWire);
        public int GetHashCode();
    }

    public interface IInputModernWire: IModernWire
    {
        internal void InternalConnectOutput(IOutputModernWire outputWire);
        internal void InternalDisconnectOutput(IOutputModernWire outputWire);
        bool IsConnectedIn(IOutputModernWire outputWire);
    }
    public interface IOutputModernWire: IModernWire
    {
        internal void InternalConnectInput(IInputModernWire inputWire);
        internal void InternalDisconnectInput(IInputModernWire inputWire);
        
        bool IsConnectedOut(IInputModernWire inputWire);
    }
    public interface IInputModernWire<TOutputModernWire> : IInputModernWire
        where TOutputModernWire : IOutputModernWire
    {
        /// <summary>
        /// Connect another wire to this wire as input (output wire will be connected to this wire as output)
        /// </summary>
        /// <param name="outputWire"></param>
        /// <param name="disconnectOthers">Disconnect others wires when connect this one</param>
        /// <returns></returns>
        DisposableAction ConnectIn(TOutputModernWire outputWire);
    }

    // Общий базовый интерфейс для выходных проводов
    public interface IOutputModernWire<TInputModernWire> : IOutputModernWire
        where TInputModernWire : IInputModernWire
    {
        /// <summary>
        /// Connect this wire to another wire as output (input wire will be connected to this wire as input)
        /// </summary>
        /// <param name="inputWire"></param>
        /// <param name="disconnectOthers">Disconnect others wires when connect this one</param>
        /// <returns></returns>
        DisposableAction ConnectOut(TInputModernWire inputWire);
    }
}