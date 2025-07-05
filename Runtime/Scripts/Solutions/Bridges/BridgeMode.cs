namespace Metamatic
{
    /// <summary>
    /// Режим работы моста.
    /// Default – текущее поведение (единоразовая таска).
    /// Buffering – все сообщения накапливаются в памяти.
    /// </summary>
    public enum BridgeMode
    {
        Default,
        Buffering
    }
}