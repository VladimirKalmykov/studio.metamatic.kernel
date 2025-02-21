using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Metamatic.SaveSystem
{
    /// <summary>
    /// This serialization JSON converts support type versioning and backward compatibility (migration) from the previous versions.
    /// All classes or structures that have [Version()] attribute will be serialized in a special way which make it possible to deserialize
    /// them with backward compatibility to previous versions. Otherwise, this converter will serialize object's type, thus you can serialize
    /// an array of objects with one common base class, but different derived classes (but ensure that base class and derived classes also have [Version()] attribute).
    /// </summary>
    public class SerializationVersionControlConverter: JsonConverter
    {
        private readonly List<Type> m_Types;
        private readonly List<string> m_Glossary;
        public SerializationVersionControlConverter(List<Type> types, List<string> glossary)
        {
            m_Types = types;
            m_Glossary = glossary;
        }
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var jRoot = new JObject();
            var version = VersionAttribute.GetVersion(value.GetType(), "0");
            /* Check if the version is in the glossary */
            var versionIndex = m_Glossary.IndexOf(version);
            if (versionIndex == -1)
            {
                m_Glossary.Add(version);
                versionIndex = m_Glossary.Count - 1;
            }
            jRoot["v"] = versionIndex;
            
            /* Check if the type is in the types */
            var typeIndex = m_Types.IndexOf(value.GetType());
            if (typeIndex == -1)
            {
                m_Types.Add(value.GetType());
                typeIndex = m_Types.Count - 1;
            }
            jRoot["t"] = typeIndex;
            jRoot["d"] = JToken.FromObject(value, serializer);
            jRoot.WriteTo(writer);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jRoot = JObject.Load(reader);
            var versionIndex = jRoot["v"]?.ToObject<int>() ?? -1;
            var typeIndex = jRoot["t"]?.ToObject<int>() ?? -1;
            
            if ((versionIndex == -1 || m_Glossary.Count <= versionIndex) || (typeIndex == -1 || m_Types.Count <= typeIndex))
            {
                Console.Error("SerializationVersionControl", $"Version or type index is out of range. Version index: {versionIndex}, Type index: {typeIndex}");
                return null;
            }
            
            /* Resolve serialized type and version */
            var version = m_Glossary[versionIndex];
            var savedType = m_Types[typeIndex];
            
            var data = jRoot["d"];
            var type = VersionAttribute.GetTypeForSpecifiedVersion(savedType, version);

            if (type == null)
            {
                Console.Error("SerializationVersionControl", $"Type {savedType} does not have backward compatibility for version {version}");
                return null;
            }
            
            var oldData = data.ToObject(type, serializer);
            var currentVersion = VersionAttribute.GetVersion(type, "0");
            if (version != currentVersion)
            {
                return Migrate(type, version, oldData);
            }

            return oldData;
        }

        public override bool CanConvert(Type objectType)
        {
            return VersionAttribute.GetVersion(objectType, "0") != "0" || DerivedVersionControlAttribute.IsDerivedVersionControl(objectType);
        }
        
        private static object Migrate(Type type, string version, object data)
        {
            var migrateMethod = type.GetMethod("Migrate", BindingFlags.Static | BindingFlags.Public);
            if (migrateMethod != null)
            {
                return migrateMethod.Invoke(null, new object[] { version, data });
            }
            Console.Error("SerializationVersionControl", $"Type {type} does not have a migration method for version {version}");

            return default;
        }
    }
}