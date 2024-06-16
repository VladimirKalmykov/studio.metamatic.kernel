using System.Threading;
using Cysharp.Threading.Tasks;

namespace Metamatic.StorageDriver
{
    public interface IStorageDriver
    {
        public Storage storage { get; }
    }
    public abstract class StorageDriver: IStorageDriver
    {
        protected readonly Storage m_Storage;
        public Storage storage => m_Storage;
        public readonly Repository repository;
        protected StorageDriver(Repository repository, Storage storage)
        {
            this.repository = repository;
            m_Storage = storage;
        }

        public abstract UniTask<(T, Storage.Metadata)> Get<T>(string address, CancellationToken cancellationToken);
        public abstract UniTask Set<T>(string address, T value, CancellationToken cancellationToken);
        
    }
}