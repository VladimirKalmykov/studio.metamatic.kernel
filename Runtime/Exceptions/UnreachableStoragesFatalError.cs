namespace Metamatic.Exceptions
{
    public class UnreachableStoragesFatalError: FatalError
    {
        public UnreachableStoragesFatalError(string message) : base(message)
        {
        }
    }
}