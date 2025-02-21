using System;
using System.Reflection;
using UnityEngine;

namespace Metamatic
{
    /// <summary>
    /// This class is used to store data in the persistent way. Within the repository, data is stored in a serializable format. It is also used to migrate data between versions.
    /// You must provide a version number for each data type. The version number is used to migrate data between versions. Use the <see cref="VersionAttribute"/> to specify the version number.
    /// </summary>
    public class VersionedDataTypeContainer: ISerializationCallbackReceiver
    {
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
        
        /// <summary>
        /// Serialize object to packed data serialized to string. The packed data contains information about object's class version, that helps to migrate data after deserialization.
        /// </summary>
        /// <param name="value"></param>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TMetadata"></typeparam>
        /// <returns></returns>
        public static string SerializeJson<T, TMetadata>(T value, TMetadata metadata)
        {
            var package = new VersionedDataTypeContainer<T, TMetadata>(value, metadata);
            var serialized = JsonUtility.ToJson(package);
            return serialized;
        }
        
        /// <summary>
        /// Deserialize raw result to the specified type. This method support migration from old version to the new.
        /// The new class or structure must have [VersionAttribute], which specifying current version
        /// </summary>
        /// <param name="rawResult"></param>
        /// <typeparam name="T"></typeparam>
        public static (T, Metadata, TMetadata) DeserializeJson<T, TMetadata>(string rawResult)
        {
            var data = JsonUtility.FromJson<VersionedDataTypeContainer<T, TMetadata>>(rawResult);

            if (data.metadata == null || data.metadata.typeVersion == null)
            {
                throw new SimpleStorage.NotFoundException("Data is corrupted or does not contain metadata");
            }
            
            if (data.metadata.typeVersion != VersionAttribute.GetVersion<T>())
            {
                /* Migrate */
                var oldVersionType = VersionAttribute.GetTypeForSpecifiedVersion<T>(data.metadata.typeVersion);
                Debug.Log($"Old version type is {oldVersionType}");
                Debug.Assert(oldVersionType != null, "Old version type is not supported");
                if (oldVersionType != null)
                {
                    Type genericType = typeof(VersionedDataTypeContainer<,>);
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

        public void OnBeforeSerialize()
        {
            throw new NotImplementedException();
        }

        public void OnAfterDeserialize()
        {
            throw new NotImplementedException();
        }
    }

    [Serializable]
    public class VersionedDataTypeContainer<T, TMetadata> : VersionedDataTypeContainer
    {
        public Metadata<TMetadata> metadata;
        public T value;

        public VersionedDataTypeContainer()
        {
            value = default;
            metadata = null;
        }

        public VersionedDataTypeContainer(T value, TMetadata customMetadata)
        {
            this.value = value;
            /* Read version from VersionAttribute of T, if exists. Or use default version: 0.0 */
            var version = VersionAttribute.GetVersion<T>();
            metadata = new Metadata<TMetadata>(version, DateTime.UtcNow.Ticks, customMetadata);
        }

        public static Metadata CreateMetadata(T value, TMetadata customMetadata)
        {
            var version = VersionAttribute.GetVersion<T>();
            return new Metadata<TMetadata>(version, DateTime.UtcNow.Ticks, customMetadata);
        }
    }
}