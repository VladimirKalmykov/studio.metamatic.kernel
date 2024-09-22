using System;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic
{
    public class DataTypeMigrationException : Exception
    {
        public DataTypeMigrationException(string message) : base(message)
        {
        }
    }
    
    /// <summary>
    /// Base class for all storage types. Storages are used to store data in different sources, like local memory, file system, network, etc.
    /// It provides the same interface for all storage types, so you can use any storage type without changing the code.
    /// Also storages throw exceptions, that are common for all storage types, so you can handle them in the same way.
    /// NotFoundException - thrown when connection is successful, but requested resource is not found
    /// UnreachableException - thrown when connection is impossible for some reasons (i.e. timeout, connection refused)
    /// ConnectionRefusedException - thrown when connection is refused for some reasons (in future, it will be separated to UnauthorizedException, ForbiddenException, etc)
    /// </summary>
    public abstract class StorageLegacy
    {
        #region Exceptions
        public abstract class IOException: Exception
        {
            public IOException(string message): base(message)
            {
                
            }
        }
        
        /// <summary>
        /// Connection completed successfully, but requested resource is not found
        /// </summary>
        public class NotFoundException: IOException
        {
            public NotFoundException(string message): base(message)
            {
                
            }
        }
        
        /// <summary>
        /// Determines that requested resource is unreachable for some reasons
        /// </summary>
        public class UnreachableException: IOException
        {
            public UnreachableException(string message): base(message)
            {
                
            }
        }
        
        /// <summary>
        /// Connection timeout
        /// </summary>
        public class TimeoutException: UnreachableException
        {
            public TimeoutException(string message): base(message)
            {
                
            }
        }
        
        /// <summary>
        /// Storage is disabled
        /// </summary>
        public class DisabledException: UnreachableException
        {
            public DisabledException(): base("Storage is disabled")
            {
                
            }
        }
        
        /// <summary>
        /// Connection refused for some reasons
        /// </summary>
        public class ConnectionRefusedException: UnreachableException
        {
            public ConnectionRefusedException(string message): base(message)
            {
                
            }
        }
        #endregion
        
        #region Structures and classes
        public enum LocationType
        {
            Local,
            Remote
        }
        
        [Serializable]
        public class Metadata
        {
            public string typeVersion;
            public long updateTime;
            
            public Metadata(string typeVersion, long updateTime)
            {
                this.typeVersion = typeVersion;
                this.updateTime = updateTime;
            }
        }

        [Serializable]
        public class Metadata<TCustom> : Metadata
        {
            public TCustom custom;
            public Metadata(string typeVersion, long updateTime, TCustom custom) : base(typeVersion, updateTime)
            {
                this.custom = custom;
            }
        }
        #endregion
        
        public bool enabled = true;
        
        public readonly UriLockerManager lockerManager = new UriLockerManager();
        public abstract UniTask<(TData data, Metadata metadata, TMetadata customMetadata)> Get<TData, TMetadata>(string address, CancellationToken token);

        public async UniTask<(TData data, Metadata metadata)> Get<TData>(string address, CancellationToken token)
        {
             var (data, metadata, customMetadata) = await Get<TData, object>(address, token);
             return (data, metadata);
        }
        public abstract UniTask Set<TData, TMetadata>(string address, TData value, TMetadata metadata, CancellationToken token);

        public UniTask Set<TData>(string address, TData value, CancellationToken token)
        {
            return Set<TData, object>(address, value, null, token);
        }

        public abstract UniTask Delete<TData>(string address, CancellationToken token);
        /// <summary>
        /// Location type determines where the data is stored. It can be local or remote (local by default).
        /// Set LocationType.Remote to apply possible synchronization logic for remote storages.
        /// </summary>
        public LocationType locationType = LocationType.Local;


        /// <summary>
        /// Serialize object to packed data serialized to string. The packed data contains information about object's class version, that helps to migrate data after deserialization.
        /// </summary>
        /// <param name="value"></param>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TMetadata"></typeparam>
        /// <returns></returns>
        public static string Serialize<T, TMetadata>(T value, TMetadata metadata)
        {
            var package = new SerializableDataPackageLegacy<T, TMetadata>(value, metadata);
            var serialized = JsonUtility.ToJson(package);
            return serialized;
        }
        
        /// <summary>
        /// Deserialize raw result to the specified type. This method support migration from old version to the new.
        /// The new class or structure must have [VersionAttribute], which specifying current version
        /// </summary>
        /// <param name="rawResult"></param>
        /// <typeparam name="T"></typeparam>
        public static (T, Metadata, TMetadata) Deserialize<T, TMetadata>(string rawResult)
        {
            var data = JsonUtility.FromJson<SerializableDataPackageLegacy<T, TMetadata>>(rawResult);

            if (data.metadata == null || data.metadata.typeVersion == null)
            {
                throw new NotFoundException("Data is corrupted or does not contain metadata");
            }
            
            if (data.metadata.typeVersion != VersionAttribute.GetVersion<T>())
            {
                /* Migrate */
                var oldVersionType = VersionAttribute.GetTypeForSpecifiedVersion<T>(data.metadata.typeVersion);
                Debug.Log($"Old version type is {oldVersionType}");
                Debug.Assert(oldVersionType != null, "Old version type is not supported");
                if (oldVersionType != null)
                {
                    Type genericType = typeof(SerializableDataPackageLegacy<,>);
                    Type constructedType = genericType.MakeGenericType(oldVersionType, typeof(TMetadata));
                    var oldData = JsonUtility.FromJson(rawResult, constructedType);
                    if (oldData == null)
                    {
                        throw new DataTypeMigrationException($"Data of type {typeof(T).Name}:{data.metadata.typeVersion} is corrupted or does not match the represented type");
                    }
                    var valueProperty = oldData.GetType().GetField("value");
                    if (valueProperty != null)
                    {
                        object value = valueProperty.GetValue(oldData);
                        
                        return (Migrate<T>(data.metadata.typeVersion, value), data.metadata, data.metadata.custom);
                    }

                    throw new DataTypeMigrationException($"Data of type {typeof(T).Name}:{data.metadata.typeVersion} is corrupted or does not match the represented type");
                }
                else
                {
                    throw new DataTypeMigrationException($"Data of type {typeof(T).Name}:{data.metadata.typeVersion} has no type representation in current assembly");
                }
            }
            else
            {
                return (data.value, data.metadata, data.metadata.custom);
            }
        }
        
        private static T Migrate<T>(string version, object data)
        {
            var migrateMethod = typeof(T).GetMethod("Migrate", BindingFlags.Static | BindingFlags.Public);
            if (migrateMethod != null)
            {
                return (T)migrateMethod.Invoke(null, new object[] { version, data });
            }

            return default(T);
        }
    }
}