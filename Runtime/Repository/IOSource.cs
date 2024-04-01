using System.Threading;

namespace Metamatic
{
    public abstract class IOSource
    {
        public readonly UriLockerManager lockerManager = new UriLockerManager();
        public abstract AsyncReadOperation<T> Read<T>(string name, CancellationToken token);
        public abstract AsyncWriteOperation<T> Write<T>(string uri, T value, CancellationToken token);
    }
}