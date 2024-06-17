using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Metamatic.StorageDriver
{
    public class ProxyStorageDriver: StorageDriver
    {
        public ProxyStorageDriver(Repository repository, Storage storage) : base(repository, storage)
        {
        }

        public override async UniTask<(T, Storage.Metadata, TMetadata)> Get<T, TMetadata>(string address, CancellationToken cancellationToken)
        {
            int retryCount = 0;
            while (true)
            {
                try
                
                {
                    return await repository.ManageRequest<T, TMetadata>(storage, address, m_Storage.Get<T, TMetadata>(address, cancellationToken), retryCount);
                }
                catch (Repository.RetryRequiredException)
                {
                    retryCount++;
                    /* Delay one second before retrying */
                    await UniTask.Delay(TimeSpan.FromMilliseconds(1000), true, PlayerLoopTiming.TimeUpdate, cancellationToken);
                    continue;
                }
                catch (Exception e)
                {
                    throw e;
                }
            }
        }

        public override UniTask Set<T, TMetadata>(string address, T value, TMetadata metadata, CancellationToken cancellationToken)
        {
            // TODO: Add request monitoring
            return m_Storage.Set(address, value, metadata, cancellationToken);
        }

        public override UniTask Delete<T>(string address, CancellationToken cancellationToken)
        {
            // TODO: Add request monitoring
            return m_Storage.Delete<T>(address, cancellationToken);
        }
    }
}