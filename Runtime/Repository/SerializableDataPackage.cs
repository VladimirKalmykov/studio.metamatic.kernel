using System;
using UnityEngine.Serialization;

namespace Metamatic
{
    /// <summary>
    /// This class is used to store data in the repository. Within the repository, data is stored in a serializable format. It is also used to migrate data between versions.
    /// You must provide a version number for each data type. The version number is used to migrate data between versions. Use the <see cref="VersionAttribute"/> to specify the version number.
    /// </summary>
    public class SerializableDataPackage
    {
        
    }
    [Serializable]
    public class SerializableDataPackage<T, TMetadata>: SerializableDataPackage
    {
        public readonly Storage.Metadata<TMetadata> metadata;
        public T value;
        public SerializableDataPackage(T value, TMetadata customMetadata) {
            this.value = value;
            /* Read version from VersionAttribute of T, if exists. Or use default version: 0.0 */
            var version = VersionAttribute.GetVersion<T>();
            metadata = new Storage.Metadata<TMetadata>(version, DateTime.UtcNow.Ticks, customMetadata);
        }

        public static Storage.Metadata GetMetadata(T value, TMetadata customMetadata)
        {
            var version = VersionAttribute.GetVersion<T>();
            return new Storage.Metadata<TMetadata>(version, DateTime.UtcNow.Ticks, customMetadata); 
        }
    }
}


