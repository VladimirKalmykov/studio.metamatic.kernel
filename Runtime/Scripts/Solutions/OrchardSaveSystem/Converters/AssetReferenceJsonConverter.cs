using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine.AddressableAssets;

public class AssetReferenceJsonConverter : JsonConverter
{
    private readonly List<Type> m_Types;
    private readonly List<string> m_Glossary;
    public AssetReferenceJsonConverter(List<Type> types, List<string> glossary)
    {
        m_Types = types;
        m_Glossary = glossary;
    }
    public override bool CanConvert(Type objectType)
    {
        return typeof(AssetReference).IsAssignableFrom(objectType);
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        var assetReference = value as AssetReference;
        if (assetReference == null)
        {
            writer.WriteNull();
            return;
        }
        
        var jRoot = new JObject();
        
        var assetGuid = assetReference.AssetGUID;
        /* Check if the assetGuid is in the glossary */
        var assetGuidIndex = m_Glossary.IndexOf(assetGuid);
        if (assetGuidIndex == -1)
        {
            m_Glossary.Add(assetGuid);
            assetGuidIndex = m_Glossary.Count - 1;
        }
        
        jRoot["guid"] = assetGuidIndex;

        var type = assetReference.GetType();
        
        /* Check if the type is in the types */
        var typeIndex = m_Types.IndexOf(type);
        if (typeIndex == -1)
        {
            m_Types.Add(type);
            typeIndex = m_Types.Count - 1;
        }
        jRoot["t"] = typeIndex;

        jRoot.WriteTo(writer);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        // Если в JSON записан null, то сразу возвращаем null
        if (reader.TokenType == JsonToken.Null)
        {
            return null;
        }
        var jRoot = JObject.Load(reader);
        var assetGuidIndex = jRoot["guid"]?.ToObject<int>() ?? -1;
        var typeIndex = jRoot["t"]?.ToObject<int>() ?? -1;
        
        if ((assetGuidIndex == -1 || m_Glossary.Count <= assetGuidIndex) || (typeIndex == -1 || m_Types.Count <= typeIndex))
        {
            Metamatic.Console.Error("AssetReferenceJsonConverter", $"AssetGuid or type index is out of range. AssetGuid index: {assetGuidIndex}, Type index: {typeIndex}");
            return null;
        }
        
        /* Resolve serialized type and assetGuid */
        var assetGuid = m_Glossary[assetGuidIndex];
        var savedType = m_Types[typeIndex];
        
        // Читаем строку, которая хранит GUID
        var guid = reader.Value as string;
        if (string.IsNullOrEmpty(guid))
        {
            return null;
        }

        // Создаём экземпляр AssetReference (или наследника) с этим GUID.
        // Для базового AssetReference:
        if (savedType == typeof(AssetReference))
        {
            return new AssetReference(guid);
        }
        else
        {
            // Если у вас есть наследники вроде AssetReferenceT<T>, 
            // можно вызвать Activator.CreateInstance.
            // Предполагается, что есть конструктор (string guid).
            // Иначе придётся искать нужный конструктор через рефлексию.

            return Activator.CreateInstance(savedType, guid);
        }
    }
}