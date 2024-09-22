using System;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic.StorageDriver
{
    public class ProxyStorageDriver: StorageDriver
    {
        /// <summary>
        /// Timeout for the request. If the request is not completed within this time, it will throw a TimeoutException
        /// </summary>
        public float timeout => repository.timeout;
        public ProxyStorageDriver(RepositoryLegacy repository, StorageLegacy storage) : base(repository, storage)
        {
            
        }

        public override async UniTask<(T, StorageLegacy.Metadata, TMetadata)> Get<T, TMetadata>(string address, CancellationToken cancellationToken)
        {
            int retryCount = 0;
            while (true)
            {
                try
                {
                    return await repository.ManageGetRequest<T, TMetadata>(storage, address, InternalGet<T, TMetadata>(address, cancellationToken), retryCount);
                }
                catch (RepositoryLegacy.RetryIsRequiredException)
                {
                    retryCount++;
                    /* Delay one second before retrying */
                    await UniTask.Delay(TimeSpan.FromMilliseconds(1000), true, PlayerLoopTiming.TimeUpdate, cancellationToken);
                    continue;
                }
                catch (RepositoryLegacy.DisableStorageIsRequiredException dsire)
                {
                    DisableStorageForATime(dsire.duration, dsire.causeException);
                    throw dsire.causeException;
                }
            }
        }
        
        /// <summary>
        /// Internal get method that is used to get the data from the storage with a timeout.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="cancellationToken"></param>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TMetadata"></typeparam>
        /// <returns></returns>
        /// <exception cref="TimeoutException"></exception>
        private async UniTask<(T, StorageLegacy.Metadata, TMetadata)> InternalGet<T, TMetadata>(string address,
            CancellationToken cancellationToken)
        {
            var timeoutCancellation = new CancellationTokenSource();
            var timeoutDisposable = timeoutCancellation.CancelAfterSlim(TimeSpan.FromSeconds(timeout));
            var combinetToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCancellation.Token).Token;
            try
            {
                var result = await m_Storage.Get<T, TMetadata>(address, combinetToken);
                return result;
            }
            catch (OperationCanceledException e)
            {
                if (timeoutCancellation.IsCancellationRequested)
                {
                    throw new StorageLegacy.TimeoutException("Request timeout");
                }

                throw;
            }
            finally
            {
                timeoutDisposable.Dispose();
            }
        }

        public override async UniTask Set<T, TMetadata>(string address, T value, TMetadata metadata, CancellationToken cancellationToken)
        {
            int retryCount = 0;
            while (true)
            {
                try
                {
                    await repository.ManageSetRequest<T, TMetadata>(storage, address,
                        InternalSet(address, value, metadata, cancellationToken), retryCount);
                    return;
                }
                catch (RepositoryLegacy.RetryIsRequiredException rre)
                {
                    retryCount++;
                    /* Delay one second before retrying */
                    await UniTask.Delay(TimeSpan.FromSeconds(rre.delay), true, PlayerLoopTiming.TimeUpdate,
                        cancellationToken);
                    continue;
                }
                catch (RepositoryLegacy.DisableStorageIsRequiredException dsire)
                {
                    DisableStorageForATime(dsire.duration, dsire.causeException);
                    throw dsire.causeException;
                }
                catch (Exception e)
                {
                    throw;
                }
            }
        }
        
        /// <summary>
        /// Internal set method that is used to set the data to the storage with a timeout.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="value"></param>
        /// <param name="metadata"></param>
        /// <param name="cancellationToken"></param>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TMetadata"></typeparam>
        /// <exception cref="TimeoutException"></exception>
        private async UniTask InternalSet<T, TMetadata>(string address, T value, TMetadata metadata, CancellationToken cancellationToken)
        {
            var timeoutCancellation = new CancellationTokenSource();
            var timeoutDisposable = timeoutCancellation.CancelAfterSlim(TimeSpan.FromSeconds(timeout));
            var combinetToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCancellation.Token).Token;
            try
            {
                await m_Storage.Set(address, value, metadata, combinetToken);
                return;
            }
            catch (OperationCanceledException e)
            {
                if (timeoutCancellation.IsCancellationRequested)
                {
                    throw new StorageLegacy.TimeoutException("Request timeout");
                }

                throw;
            }
            finally
            {
                timeoutDisposable.Dispose();
            }
        }

        public override async UniTask Delete<T>(string address, CancellationToken cancellationToken)
        {
            int retryCount = 0;
            while (true)
            {
                try
                {
                    await repository.ManageDeleteRequest<T>(storage, address, InternalDelete<T>(address, cancellationToken), retryCount);
                    return;
                }
                catch (RepositoryLegacy.RetryIsRequiredException)
                {
                    retryCount++;
                    /* Delay one second before retrying */
                    await UniTask.Delay(TimeSpan.FromMilliseconds(1000), true, PlayerLoopTiming.TimeUpdate, cancellationToken);
                    continue;
                }
                
                catch (RepositoryLegacy.DisableStorageIsRequiredException dsire)
                {
                    DisableStorageForATime(dsire.duration, dsire.causeException);
                    throw dsire.causeException;
                }
                catch (Exception e)
                {
                    throw;
                }
            }
        }
        
        /// <summary>
        /// Internal delete method that is used to delete the data from the storage with a timeout.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="cancellationToken"></param>
        /// <typeparam name="T"></typeparam>
        /// <exception cref="TimeoutException"></exception>
        private async UniTask InternalDelete<T>(string address, CancellationToken cancellationToken)
        {
            var timeoutCancellation = new CancellationTokenSource();
            var timeoutDisposable = timeoutCancellation.CancelAfterSlim(TimeSpan.FromSeconds(timeout));
            var combinetToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCancellation.Token).Token;
            try
            {
                await m_Storage.Delete<T>(address, combinetToken);
            }
            catch (OperationCanceledException e)
            {
                if (timeoutCancellation.IsCancellationRequested)
                {
                    throw new StorageLegacy.TimeoutException("Request timeout");
                }

                throw;
            }
            finally
            {
                timeoutDisposable.Dispose();
            }
        }
        
        private void DisableStorageForATime(float duration, Exception reasonException)
        {
            Console.Exception("StorageDriver",  $"Disable storage {storage.GetType().Name} for {duration} seconds because of {reasonException.Message}");
            storage.enabled = false;
            UniTask.RunOnThreadPool(async () =>
            {
                /* Wait for the duration */
                await Task.Delay(TimeSpan.FromSeconds(duration)).ConfigureAwait(true);
                await UniTask.SwitchToMainThread();
                storage.enabled = true;
                Console.Exception("StorageDriver",  $"Enable storage {storage.GetType().Name} after {duration} seconds delay");
            }, true, repository.disposeToken).Forget();
        }
    }
}