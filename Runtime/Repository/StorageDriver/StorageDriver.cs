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

        public abstract UniTask<(T, Storage.Metadata, TMetadata)> Get<T, TMetadata>(string address, CancellationToken cancellationToken);
        public async UniTask<(TData data, Storage.Metadata metadata)> Get<TData>(string address, CancellationToken token)
        {
            var (data, metadata, customMetadata) = await Get<TData, object>(address, token);
            return (data, metadata);
        }

        public abstract UniTask Set<T, TMetadata>(string address, T value, TMetadata metadata, CancellationToken cancellationToken);
        public UniTask Set<TData>(string address, TData value, CancellationToken token)
        {
            return Set<TData, object>(address, value, null, token);
        }
        
        public abstract UniTask Delete<T>(string address, CancellationToken cancellationToken);
    }
}