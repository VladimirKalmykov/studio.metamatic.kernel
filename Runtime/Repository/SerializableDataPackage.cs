using System;

namespace Metamatic
{
    /// <summary>
    /// This class is used to store data in the repository. Within the repository, data is stored in a serializable format. It is also used to migrate data between versions.
    /// You must provide a version number for each data type. The version number is used to migrate data between versions. Use the <see cref="VersionAttribute"/> to specify the version number.
    /// </summary>
    public class SerializableDataPackage
    {
        [Serializable]
        public struct Metadata
        {
            public string typeVersion;
        }
    }
    [Serializable]
    public class SerializableDataPackage<T>: SerializableDataPackage
    {
        public Metadata metadata;
        public T value;
        public SerializableDataPackage(T value, Metadata metadata = default) {
            this.value = value;
            /* Read version from VersionAttribute of T, if exists. Or use default version: 0.0 */
            var version = VersionAttribute.GetVersion<T>("0.0");
            this.metadata.typeVersion = version;
        }
    }
}


