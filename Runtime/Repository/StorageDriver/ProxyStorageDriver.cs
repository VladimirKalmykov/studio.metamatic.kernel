using System.Threading;
using Cysharp.Threading.Tasks;

namespace Metamatic.StorageDriver
{
    public class ProxyStorageDriver: StorageDriver
    {
        public ProxyStorageDriver(Repository repository, Storage storage) : base(repository, storage)
        {
        }

        public override UniTask<(T, Storage.Metadata, TMetadata)> Get<T, TMetadata>(string address, CancellationToken cancellationToken)
        {
            return m_Storage.Get<T, TMetadata>(address, cancellationToken);
        }

        public override UniTask Set<T, TMetadata>(string address, T value, TMetadata metadata, CancellationToken cancellationToken)
        {
            return m_Storage.Set(address, value, metadata, cancellationToken);
        }
    }
}