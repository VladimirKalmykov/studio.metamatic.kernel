using System.Threading;
using Cysharp.Threading.Tasks;

namespace Metamatic.StorageDriver
{
    public class ProxyStorageDriver: StorageDriver
    {
        public ProxyStorageDriver(Repository repository, Storage storage) : base(repository, storage)
        {
        }

        public override UniTask<(T, Storage.Metadata)> Get<T>(string address, CancellationToken cancellationToken)
        {
            return m_Storage.Get<T>(address, cancellationToken);
        }

        public override UniTask Set<T>(string address, T value, CancellationToken cancellationToken)
        {
            return m_Storage.Set<T>(address, value, cancellationToken);
        }
    }
}