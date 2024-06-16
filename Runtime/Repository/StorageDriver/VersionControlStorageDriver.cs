using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic.StorageDriver
{
    public struct VersionedData<T>
    {
        public T data;
        public long version;
        
        public VersionedData(T data, long version)
        {
            this.data = data;
            this.version = version;
        }
    }
    
    public interface IVersionControlStorageDriver: IStorageDriver
    {
        UniTask<(VersionedData<T>, Storage.Metadata)> Get<T>(string address, CancellationToken cancellationToken);
        UniTask Set<T>(string address, T value, long version, CancellationToken cancellationToken);
    }
    
    /// <summary>
    /// It responsible for versioning the data.
    /// </summary>
    public class VersionControlStorageDriver: ProxyStorageDriver, IVersionControlStorageDriver
    {
        public VersionControlStorageDriver(Repository repository, Storage storage) : base(repository, storage)
        {
        }
        
        public UniTask<(VersionedData<T>, Storage.Metadata)> Get<T>(string address, CancellationToken cancellationToken)
        {
            return base.Get<VersionedData<T>>(address, cancellationToken);
        }

        public UniTask Set<T>(string address, T value, long version, CancellationToken cancellationToken)
        {
            var packedValue = new VersionedData<T>(value, version);
            return base.Set<VersionedData<T>>(address, packedValue, cancellationToken);
        }
    }
}