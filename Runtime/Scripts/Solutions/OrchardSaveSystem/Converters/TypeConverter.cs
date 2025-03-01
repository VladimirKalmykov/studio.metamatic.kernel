using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Metamatic.SaveSystem
{
    /// <summary>
    /// Converter that serializes and deserializes types to and from their indexes in the list of types.
    /// </summary>
    public class TypeConverter: JsonConverter
    {
        private readonly List<Type> m_Types;
        private readonly List<string> m_Glossary;
        public TypeConverter(List<Type> types)
        {
            m_Types = types;
        }
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var typeIndex = m_Types.IndexOf((Type) value);
            if (typeIndex == -1)
            {
                m_Types.Add((Type) value);
                typeIndex = m_Types.Count - 1;
            }
            writer.WriteValue(typeIndex);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var typeIndex = Convert.ToInt32(reader.Value);
            if (m_Types.Count <= typeIndex)
            {
                Console.Error("TypeConverter", $"Type index is out of range. Type index: {typeIndex}");
                return null;
            }
            return m_Types[typeIndex];
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Type) || objectType.IsSubclassOf(typeof(Type));
        }
    }
}