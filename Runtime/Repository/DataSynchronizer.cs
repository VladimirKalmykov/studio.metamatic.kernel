using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Metamatic;
using Metamatic.StorageDriver;
using UnityEngine;
using System.Linq;

namespace DuckJumper2024
{
    /// <summary>
    /// Storages synchronizer is a special tool that helps to synchronize data between different storages.
    /// This tool gives you the ability to keep data in sync between different sources, like local memory, file system, network, etc., that is useful for offline-first applications.
    /// </summary>
    public class DataSynchronizer
    {
        [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class, AllowMultiple = false)]
        public class UseStrategyAttribute : System.Attribute
        {
            public BiDirectionalStrategy strategy { get; }

            public UseStrategyAttribute(BiDirectionalStrategy strategy)
            {
                this.strategy = strategy;
            }
            
            public static BiDirectionalStrategy GetStrategy<T>()
            {
                var attributes = (UseStrategyAttribute[]) typeof(T).GetCustomAttributes(typeof(UseStrategyAttribute), false);
                if (attributes.Length == 0)
                {
                    return BiDirectionalStrategy.GreaterVersionWins;
                }
                return attributes[0].strategy;
            }
        }
        public enum BiDirectionalStrategy
        {
            /// <summary>
            /// No matter what, server version always overrides client version
            /// </summary>
            RemoteWins,
            /// <summary>
            /// No matter what, client version always overrides server version
            /// </summary>
            LocalWins,
            /// <summary>
            /// Data with greater version always overrides data with lesser version
            /// </summary>
            GreaterVersionWins,
            /// <summary>
            /// Never data always overrides older data
            /// </summary>
            TimeControl
        }
        
        private readonly List<VersionControlStorageDriver> drivers = new();
        public readonly Repository repository;
        
        public DataSynchronizer(Repository repository, Storage[] storages)
        {
            this.repository = repository;
            foreach (var storage in storages)
            {
                RegisterStorage(storage);
            }
        }
        
        public DataSynchronizer(Repository repository, VersionControlStorageDriver[] storages)
        {
            this.repository = repository;
            foreach (var storage in storages)
            {
                RegisterStorage(storage);
            }
        }
        
        
        public VersionControlStorageDriver WrapStorage(Storage storage)
        {
            return new VersionControlStorageDriver(repository, storage);
        }

        public void RegisterStorage(Storage storage)
        {
            drivers.Add(WrapStorage(storage));
        }
        
        public void RegisterStorage(VersionControlStorageDriver storage)
        {
            drivers.Add(storage);
        }

        public async UniTask<(T data, Storage.Metadata metadata, long version)> Sync<T>(string address, Store<T>.GetDefaultValue getDefaultValue, CancellationToken token)
        {
            /* Get StorageSynchronizer.UseStrategy attribute */
            var strategy = UseStrategyAttribute.GetStrategy<T>();
            
            var tasks = drivers.Select(driver => GetData<T>(driver, address, token));
            var results = await UniTask.WhenAll((IEnumerable<UniTask<(VersionControlStorageDriver Driver, (T data, Storage.Metadata metadata, long version) Data, Storage.Metadata Metadata, bool Success, Exception Exception)>>)tasks);

            // Фильтрация NotFound и исключение других ошибок
            var validResults = results.Where(result => result.Success).ToList();
            var notFoundDrivers = results.Where(result => result.Exception is Storage.NotFoundException).Select(result => result.Driver).ToList();
            var successfulDrivers = validResults.Select(result => result.Driver).ToList();
            
            // Если нет успешных результатов, выбрасываем исключение
            if (!validResults.Any())
            {
                throw new InvalidOperationException("No valid data found.");
            }

            (T data, Storage.Metadata metadata, long version) bestData = default;
            (VersionControlStorageDriver Driver, (T data, Storage.Metadata metadata, long version) Data, Storage.Metadata Metadata, bool Success,
                Exception Exception)? bestSource = default;
            if (notFoundDrivers.Count == validResults.Count)
            {
                /* If there are only NotFound drivers, get default value */
                var defaultValue = await getDefaultValue(address, token);
                bestData = (defaultValue, default, 0);
            }
            else
            {
                switch (strategy)
                {
                    case BiDirectionalStrategy.GreaterVersionWins:
                        bestSource = validResults.OrderByDescending(result => result.Data.version).First();
                        bestData = bestSource.Value.Data;
                        break;
                    case BiDirectionalStrategy.RemoteWins:
                        /* Find driver with storage marked at server */
                        var remote = validResults.FirstOrDefault(result =>
                            result.Driver.storage.locationType == Storage.LocationType.Remote);
                        if (remote.Driver != null)
                        {
                            bestSource = remote;
                            bestData = remote.Data;
                            break;
                        }

                        /* If there is no server storage, find driver with the greatest version */
                        bestSource = validResults.OrderByDescending(result => result.Data.version).First();
                        bestData = bestSource.Value.Data;
                        break;
                    case BiDirectionalStrategy.LocalWins:
                        /* Find driver with storage marked as local */
                        var local = validResults.FirstOrDefault(result =>
                            result.Driver.storage.locationType == Storage.LocationType.Local);
                        if (local.Driver != null)
                        {
                            bestSource = local;
                            bestData = local.Data;
                            break;
                        }

                        /* If there is no local storage, find driver with the greatest version */
                        bestSource = validResults.OrderByDescending(result => result.Data.version).First();
                        bestData = bestSource.Value.Data;
                        break;
                    case BiDirectionalStrategy.TimeControl:
                        throw new NotImplementedException();
                }
            }
            // Найти данные с наибольшей версией
            

            // Обновить все драйверы до наибольшей версии
            var updateTasks = new List<UniTask>(successfulDrivers.Count - 1 + notFoundDrivers.Count);
            foreach (var driver in successfulDrivers)
            {
                if (bestSource.HasValue && driver == bestSource.Value.Driver)
                {
                    continue;
                }

                updateTasks.Add(UpdateData(driver, address, bestData, token));
            }

            foreach (var notFoundDriver in notFoundDrivers)
            {
                updateTasks.Add(UpdateData(notFoundDriver, address, bestData, token));
            }
            await UniTask.WhenAll(updateTasks);

            return bestData;
        }

        private async UniTask<(VersionControlStorageDriver Driver, (T data, Storage.Metadata metadata, long version) Data, bool Success, Exception Exception)> GetData<T>(VersionControlStorageDriver driver, string address, CancellationToken token)
        {
            try
            {
                var result = await driver.Get<T>(address, token);
                return (driver, result, true, null);
            }
            catch (Exception ex)
            {
                return (driver, default, false, ex);
            }
        }

        private async UniTask UpdateData<T>(VersionControlStorageDriver driver, string address, (T data, Storage.Metadata metadata, long version) data, CancellationToken token)
        {
            try
            {
                await driver.Set(address, data.data, data.version, token);
            }
            catch (Exception)
            {
                // Обработать исключения обновления, если необходимо
                throw;
            }
        }
    }
}