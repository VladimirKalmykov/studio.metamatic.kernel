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
    public abstract class IOSource
    {
        public readonly UriLockerManager lockerManager = new UriLockerManager();
        public abstract UniTask<T> Read<T>(string name, CancellationToken token);
        public abstract UniTask Write<T>(string uri, T value, CancellationToken token);
        public class NotFoundException: Exception
        {
            public NotFoundException(string message): base(message)
            {
                
            }
        }
        
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
        public static T Deserialize<T>(string rawResult)
        {
            var testData = JsonUtility.FromJson<SerializableDataPackage<T>>(rawResult);
            
            if (testData.metadata.typeVersion != VersionAttribute.GetVersion<T>())
            {
                /* Migrate */
                var oldVersionType = VersionAttribute.GetTypeForSpecifiedVersion<T>(testData.metadata.typeVersion);
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
                        
                        return Migrate<T>(testData.metadata.typeVersion, value);
                    }

                    throw new DataTypeMigrationException($"Data of type {typeof(T).Name}:{testData.metadata.typeVersion} is corrupted or does not match the represented type");
                }
                else
                {
                    throw new DataTypeMigrationException($"Data of type {typeof(T).Name}:{testData.metadata.typeVersion} has no type representation in current assembly");
                }
            }
            else
            {
                return testData.value;
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