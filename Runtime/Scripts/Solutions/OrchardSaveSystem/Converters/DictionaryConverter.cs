using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Metamatic.SaveSystem
{
    /// <summary>
    /// Конвертер, поддерживающий сериализацию и десериализацию IDictionary.
    /// Корректно взаимодействует с другими JsonConverter-ами, в том числе с версионными.
    /// </summary>
    public class DictionaryVersionControlConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is not IDictionary dictionary)
            {
                serializer.Serialize(writer, null);
                return;
            }

            writer.WriteStartArray();

            foreach (DictionaryEntry entry in dictionary)
            {
                writer.WriteStartObject();

                writer.WritePropertyName("Key");
                serializer.Serialize(writer, entry.Key);

                writer.WritePropertyName("Value");
                serializer.Serialize(writer, entry.Value);

                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var keyType = typeof(object);
            var valueType = typeof(object);

            if (objectType.IsGenericType)
            {
                var types = objectType.GetGenericArguments();
                keyType = types[0];
                valueType = types[1];
            }

            var result = (IDictionary)Activator.CreateInstance(objectType);
            var entries = serializer.Deserialize<List<Entry>>(reader);

            foreach (var entry in entries)
            {
                var key = entry.Key is null ? null : ConvertType(entry.Key, keyType);
                var value = entry.Value is null ? null : ConvertType(entry.Value, valueType);

                result.Add(key, value);
            }

            return result;
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(IDictionary).IsAssignableFrom(objectType);
        }

        /// <summary>
        /// Вспомогательный метод, корректно приводящий объект к указанному типу.
        /// </summary>
        private static object ConvertType(object obj, Type targetType)
        {
            if (obj == null)
                return null;

            if (obj.GetType() == targetType || targetType.IsInstanceOfType(obj))
                return obj;

            return Convert.ChangeType(obj, targetType);
        }

        /// <summary>
        /// Вспомогательный класс для десериализации пар ключ-значение.
        /// </summary>
        private class Entry
        {
            public object Key { get; set; }
            public object Value { get; set; }
        }
    }
}
