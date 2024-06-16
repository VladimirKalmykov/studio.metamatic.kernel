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
    [Serializable]
    public abstract class IOSourceSettings
    {
        
    }
    
    /// <summary>
    /// Base class for all storage types. Storages are used to store data in different sources, like local memory, file system, network, etc.
    /// It provides the same interface for all storage types, so you can use any storage type without changing the code.
    /// Also storages throw exceptions, that are common for all storage types, so you can handle them in the same way.
    /// NotFoundException - thrown when connection is successful, but requested resource is not found
    /// UnreachableException - thrown when connection is impossible for some reasons (i.e. timeout, connection refused)
    /// ConnectionRefusedException - thrown when connection is refused for some reasons (in future, it will be separated to UnauthorizedException, ForbiddenException, etc)
    /// </summary>
    public abstract class Storage
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
        /// Connection refused for some reasons
        /// </summary>
        public class ConnectionRefusedException: UnreachableException
        {
            public ConnectionRefusedException(string message): base(message)
            {
                
            }
        }
        #endregion
        
        public enum LocationType
        {
            Local,
            Remote
        }
        
        [Serializable]
        public readonly struct Metadata
        {
            public readonly string typeVersion;
            public readonly long updateTime;
            
            public Metadata(string typeVersion, long updateTime)
            {
                this.typeVersion = typeVersion;
                this.updateTime = updateTime;
            }
        }
        public readonly UriLockerManager lockerManager = new UriLockerManager();
        public abstract UniTask<(T data, Metadata metadata)> Get<T>(string address, CancellationToken token);
        public abstract UniTask Set<T>(string address, T value, CancellationToken token);
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
        /// <returns></returns>
        public static string Serialize<T>(T value)
        {
            var package = new SerializableDataPackage<T>(value);
            return JsonUtility.ToJson(package);
        }
        
        /// <summary>
        /// Deserialize raw result to the specified type. This method support migration from old version to the new.
        /// The new class or structure must have [VersionAttribute], which specifying current version
        /// </summary>
        /// <param name="rawResult"></param>
        /// <typeparam name="T"></typeparam>
        public static (T, Metadata) Deserialize<T>(string rawResult)
        {
            var data = JsonUtility.FromJson<SerializableDataPackage<T>>(rawResult);
            
            if (data.metadata.typeVersion != VersionAttribute.GetVersion<T>())
            {
                /* Migrate */
                var oldVersionType = VersionAttribute.GetTypeForSpecifiedVersion<T>(data.metadata.typeVersion);
                Debug.Log($"Old version type is {oldVersionType}");
                Debug.Assert(oldVersionType != null, "Old version type is not supported");
                if (oldVersionType != null)
                {
                    Type genericType = typeof(SerializableDataPackage<>);
                    Type constructedType = genericType.MakeGenericType(oldVersionType);
                    var oldData = JsonUtility.FromJson(rawResult, constructedType);
                    var valueProperty = oldData.GetType().GetField("value");
                    if (valueProperty != null)
                    {
                        object value = valueProperty.GetValue(oldData);
                        
                        return (Migrate<T>(data.metadata.typeVersion, value), data.metadata);
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
                return (data.value, data.metadata);
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