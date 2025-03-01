using System;
using Newtonsoft.Json;
using UnityEngine;

namespace Metamatic.SaveSystem
{
    public class QuaternionJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Quaternion);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            Quaternion quat = (Quaternion)value;
            writer.WriteStartObject();
            writer.WritePropertyName("x");
            writer.WriteValue(quat.x);
            writer.WritePropertyName("y");
            writer.WriteValue(quat.y);
            writer.WritePropertyName("z");
            writer.WriteValue(quat.z);
            writer.WritePropertyName("w");
            writer.WriteValue(quat.w);
            writer.WriteEndObject();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jo = Newtonsoft.Json.Linq.JObject.Load(reader);
            float x = (float)jo["x"];
            float y = (float)jo["y"];
            float z = (float)jo["z"];
            float w = (float)jo["w"];
            return new Quaternion(x, y, z, w);
        }
    }
}