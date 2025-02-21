using System;
using Newtonsoft.Json;
using UnityEngine.AddressableAssets;

public class AssetReferenceJsonConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(AssetReference);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        // Предположим, мы храним лишь строку с GUID:
        var guid = (string)reader.Value;
        if (string.IsNullOrEmpty(guid))
            return null;

        return new AssetReference(guid);
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        var assetRef = value as AssetReference;
        if (assetRef == null)
        {
            writer.WriteNull();
            return;
        }
        
        // Предположим, мы хотим писать GUID
        writer.WriteValue(assetRef.AssetGUID);
    }
}