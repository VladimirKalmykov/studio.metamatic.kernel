using System;

namespace Metamatic
{
    /// <summary>
    /// This class is used to store data in the persistent way. Within the repository, data is stored in a serializable format. It is also used to migrate data between versions.
    /// You must provide a version number for each data type. The version number is used to migrate data between versions. Use the <see cref="VersionAttribute"/> to specify the version number.
    /// </summary>
    public class SerializableDataPackageLegacy
    {
        
    }
    [Serializable]
    public class SerializableDataPackageLegacy<T, TMetadata>: SerializableDataPackageLegacy
    {
        public StorageLegacy.Metadata<TMetadata> metadata;
        public T value;

        public SerializableDataPackageLegacy()
        {
            value = default;
            metadata = null;
        }
        public SerializableDataPackageLegacy(T value, TMetadata customMetadata) {
            this.value = value;
            /* Read version from VersionAttribute of T, if exists. Or use default version: 0.0 */
            var version = VersionAttribute.GetVersion<T>();
            metadata = new StorageLegacy.Metadata<TMetadata>(version, DateTime.UtcNow.Ticks, customMetadata);
        }

        public static StorageLegacy.Metadata GetMetadata(T value, TMetadata customMetadata)
        {
            var version = VersionAttribute.GetVersion<T>();
            return new StorageLegacy.Metadata<TMetadata>(version, DateTime.UtcNow.Ticks, customMetadata); 
        }
    }
}


