using System.Threading;
using Cysharp.Threading.Tasks;

namespace Metamatic.StorageDriver
{
    /// <summary>
    /// It responsible for versioning the data.
    /// </summary>
    public class VersionControlStorageDriver: ProxyStorageDriver
    {
        public class DataVersionMetadata
        {
            public long version;
            public DataVersionMetadata(long version)
            {
                this.version = version;
            }
        }
        public class DataVersionMetadata<TCustom>: DataVersionMetadata
        {
            public TCustom custom;
            public DataVersionMetadata(long version, TCustom customMetadata) : base(version)
            {
                this.custom = customMetadata;
            }
        }
        public VersionControlStorageDriver(Repository repository, Storage storage) : base(repository, storage)
        {
        }
        
        public new async UniTask<(T, Storage.Metadata, TMetadata, long)> Get<T, TMetadata>(string address, CancellationToken cancellationToken)
        {
            var (data, metadata, customMetadata) = await base.Get<T, DataVersionMetadata<TMetadata>>(address, cancellationToken);
            var subCustomMetadata = customMetadata.custom;
            return (data, (Storage.Metadata)metadata, subCustomMetadata, customMetadata.version);
        }
        
        public new async UniTask<(T, Storage.Metadata, long)> Get<T>(string address, CancellationToken cancellationToken)
        {
            var (data, metadata, customMetadata, version) = await Get<T, object>(address, cancellationToken);
            return (data, metadata, version);
        }

        public new UniTask Set<T, TMetadata>(string address, T value, TMetadata metadata, long version, CancellationToken cancellationToken)
        {
            var dataVersionMetadata = new DataVersionMetadata<TMetadata>(version, metadata);
            return base.Set<T, DataVersionMetadata<TMetadata>>(address, value, dataVersionMetadata, cancellationToken);
        }
        
        public new UniTask Set<T>(string address, T value, long version, CancellationToken cancellationToken)
        {
            return Set<T, object>(address, value, null, version, cancellationToken);
        }
    }
}