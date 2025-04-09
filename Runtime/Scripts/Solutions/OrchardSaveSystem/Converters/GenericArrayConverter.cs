using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Metamatic.SaveSystem
{
    /// <summary>
    /// Конвертер для сериализации и десериализации массивов без версионирования.
    /// Позволяет корректно обрабатывать элементы массива через стандартные или дополнительные конвертеры.
    /// </summary>
    public class GenericArrayConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType.IsArray;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }
            
            Array array = (Array)value;
            writer.WriteStartArray();
            foreach (var item in array)
            {
                // Сериализуем каждый элемент с учетом всех зарегистрированных конвертеров
                serializer.Serialize(writer, item);
            }
            writer.WriteEndArray();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            // Загружаем массив из JSON
            JArray jArray = JArray.Load(reader);
            var elementType = objectType.GetElementType();
            Array array = Array.CreateInstance(elementType, jArray.Count);
            for (int i = 0; i < jArray.Count; i++)
            {
                // Десериализуем каждый элемент, используя стандартные конвертеры
                var item = jArray[i].ToObject(elementType, serializer);
                array.SetValue(item, i);
            }
            return array;
        }
    }
}