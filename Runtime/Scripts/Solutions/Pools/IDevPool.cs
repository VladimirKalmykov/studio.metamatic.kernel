namespace Metamatic
{
    public interface IDevPool
    {
        #if DEBUG
        public string displayName { get; }
        public int countInPool { get; }
        public int countOutPool { get; }
        #endif
    }
}