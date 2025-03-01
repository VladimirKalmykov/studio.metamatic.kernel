using System;
using Newtonsoft.Json;
using UnityEngine;

namespace Metamatic.SaveSystem
{
    public class Vector3JsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Vector3);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            Vector3 vector = (Vector3)value;
            writer.WriteStartObject();
            writer.WritePropertyName("x");
            writer.WriteValue(vector.x);
            writer.WritePropertyName("y");
            writer.WriteValue(vector.y);
            writer.WritePropertyName("z");
            writer.WriteValue(vector.z);
            writer.WriteEndObject();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jo = Newtonsoft.Json.Linq.JObject.Load(reader);
            float x = (float)jo["x"];
            float y = (float)jo["y"];
            float z = (float)jo["z"];
            return new Vector3(x, y, z);
        }
    }
}