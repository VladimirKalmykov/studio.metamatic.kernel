using System;
using Newtonsoft.Json;
using UnityEngine;

namespace Metamatic.SaveSystem
{
    public class Vector2JsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Vector2);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            Vector2 vector = (Vector2)value;
            writer.WriteStartObject();
            writer.WritePropertyName("x");
            writer.WriteValue(vector.x);
            writer.WritePropertyName("y");
            writer.WriteValue(vector.y);
            writer.WriteEndObject();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jo = Newtonsoft.Json.Linq.JObject.Load(reader);
            float x = (float)jo["x"];
            float y = (float)jo["y"];
            return new Vector2(x, y);
        }
    }
}