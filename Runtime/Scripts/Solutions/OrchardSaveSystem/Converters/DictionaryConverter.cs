using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Metamatic.SaveSystem
{
    /// <summary>
    /// Конвертер, поддерживающий сериализацию и десериализацию IDictionary,
    /// сериализуя словарь в виде двух массивов: Keys и Values.
    /// Корректно взаимодействует с другими JsonConverter-ами, в том числе с версионными.
    /// </summary>
    public class DictionaryVersionControlConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (!(value is IDictionary dictionary))
            {
                serializer.Serialize(writer, null);
                return;
            }
            
            writer.WriteStartObject();

            // Сериализуем массив ключей
            writer.WritePropertyName("Keys");
            writer.WriteStartArray();
            foreach (DictionaryEntry entry in dictionary)
            {
                serializer.Serialize(writer, entry.Key);
            }
            writer.WriteEndArray();

            // Сериализуем массив значений
            writer.WritePropertyName("Values");
            writer.WriteStartArray();
            foreach (DictionaryEntry entry in dictionary)
            {
                serializer.Serialize(writer, entry.Value);
            }
            writer.WriteEndArray();

            writer.WriteEndObject();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            // Определяем типы ключей и значений
            Type keyType = typeof(object);
            Type valueType = typeof(object);
            if (objectType.IsGenericType)
            {
                var types = objectType.GetGenericArguments();
                keyType = types[0];
                valueType = types[1];
            }

            var result = (IDictionary)Activator.CreateInstance(objectType);

            // Читаем объект с массивами "Keys" и "Values"
            JObject jObject = JObject.Load(reader);
            JToken keysToken = jObject["Keys"];
            JToken valuesToken = jObject["Values"];

            if (keysToken == null || valuesToken == null)
                throw new JsonSerializationException("Отсутствуют свойства Keys или Values.");

            // Создаем типы списков для ключей и значений
            Type listKeyType = typeof(List<>).MakeGenericType(keyType);
            Type listValueType = typeof(List<>).MakeGenericType(valueType);

            // Десериализуем JSON-массивы в списки нужного типа
            var keysList = (IList)keysToken.ToObject(listKeyType, serializer);
            var valuesList = (IList)valuesToken.ToObject(listValueType, serializer);

            if (keysList.Count != valuesList.Count)
                throw new JsonSerializationException("Массивы Keys и Values имеют разную длину.");

            for (int i = 0; i < keysList.Count; i++)
            {
                result.Add(keysList[i], valuesList[i]);
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
    }
}
