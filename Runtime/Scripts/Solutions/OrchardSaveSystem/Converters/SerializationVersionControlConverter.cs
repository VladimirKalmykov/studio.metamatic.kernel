using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using UnityEngine;

namespace Metamatic.SaveSystem
{
    /// <summary>
    /// This serialization JSON converts support type versioning and backward compatibility (migration) from the previous versions.
    /// All classes or structures that have [Version()] attribute will be serialized in a special way which make it possible to deserialize
    /// them with backward compatibility to previous versions. Otherwise, this converter will serialize object's type, thus you can serialize
    /// an array of objects with one common base class, but different derived classes (but ensure that base class and derived classes also have [Version()] attribute).
    /// </summary>
    public class SerializationVersionControlConverter : JsonConverter
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
            // --- 1. Сохраняем «v» и «t» как раньше ---
            var version = VersionAttribute.GetVersion(value.GetType(), "0");
            int versionIndex = m_Glossary.IndexOf(version);
            if (versionIndex == -1)
            {
                m_Glossary.Add(version);
                versionIndex = m_Glossary.Count - 1;
            }

            int typeIndex = m_Types.IndexOf(value.GetType());
            if (typeIndex == -1)
            {
                m_Types.Add(value.GetType());
                typeIndex = m_Types.Count - 1;
            }

            writer.WriteStartObject();
            writer.WritePropertyName("v");
            writer.WriteValue(versionIndex);
            writer.WritePropertyName("t");
            writer.WriteValue(typeIndex);
            writer.WritePropertyName("d");

            // --- 2. Подготовка к ручной сериализации содержимого ---
            var contract = serializer.ContractResolver.ResolveContract(value.GetType()) as JsonObjectContract;
            if (contract == null)
            {
                // Если вдруг это не объектный контракт
                serializer.Serialize(writer, value);
                writer.WriteEndObject(); // закрываем «корень»
                return;
            }

            writer.WriteStartObject();

            foreach (var jsonProp in contract.Properties)
            {
                // Проверяем, игнорируется ли поле/свойство атрибутом или настройками:
                if (jsonProp.Ignored || !jsonProp.Readable)
                    continue;

                // Уважение к логике ShouldSerialize:
                if (jsonProp.ShouldSerialize != null && !jsonProp.ShouldSerialize(value))
                    continue;

                // Определяем «чистый» MemberInfo по имени:
                MemberInfo mi = value.GetType()
                    .GetMember(jsonProp.PropertyName,
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .FirstOrDefault();

                // Если это не поле — пропускаем
                if (!(mi is FieldInfo fieldInfo))
                    continue;

                // Если само поле помечено [NonSerialized] или [JsonIgnore], пропускаем:
                if (fieldInfo.GetCustomAttribute<NonSerializedAttribute>() != null)
                    continue;
                if (fieldInfo.GetCustomAttribute<JsonIgnoreAttribute>() != null)
                    continue;

                // Всё чисто: получаем значение поля и сериализуем
                object valueOfField = fieldInfo.GetValue(value);
                writer.WritePropertyName(jsonProp.PropertyName);
                serializer.Serialize(writer, valueOfField);
            }

            writer.WriteEndObject(); // закрыли «d»-объект
            writer.WriteEndObject(); // закрыли корень (v, t, d)
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            // Загружаем корневой объект-обёртку
            JObject jRoot = JObject.Load(reader);
            int versionIndex = jRoot["v"]?.ToObject<int>() ?? -1;
            int typeIndex = jRoot["t"]?.ToObject<int>() ?? -1;

            if (versionIndex < 0 || versionIndex >= m_Glossary.Count ||
                typeIndex < 0 || typeIndex >= m_Types.Count)
            {
                Debug.Log(
                    $"Version or type index is out of range. Version index: {versionIndex}, Type index: {typeIndex}");
                return null;
            }

            // Получаем версию и сохранённый тип
            string version = m_Glossary[versionIndex];
            Type savedType = m_Types[typeIndex];

            // Определяем целевой тип для десериализации с учётом версии
            Type targetType = VersionAttribute.GetTypeForSpecifiedVersion(savedType, version);
            if (targetType == null)
            {
                Debug.Log($"Type {savedType} does not have backward compatibility for version {version}");
                return null;
            }

            // Извлекаем содержимое версии
            JToken dataToken = jRoot["d"];
            if (dataToken == null)
                return null;

            // Пытаемся получить контракт для целевого типа
            JsonObjectContract contract = serializer.ContractResolver.ResolveContract(targetType) as JsonObjectContract;
            if (contract == null)
            {
                Console.Error("SerializationVersionControl",
                    $"Type {targetType} does not have a contract resolver");
                return null;
            }

#if DEBUG
            if (contract.DefaultCreator == null)
            {
                Console.Error("SerializationVersionControl",
                    $"Type {targetType} does not have a default constructor! Probably you forgot to add parameterless constructor to the class?");
                throw new Exception(
                    $"Type {targetType} does not have a default constructor! Probably you forgot to add parameterless constructor to the class?");
            }
#endif
            // Создаем экземпляр с помощью DefaultCreator контракта
            object instance = contract.DefaultCreator();
            // Перебираем свойства, определенные контрактом
            foreach (JsonProperty property in contract.Properties)
            {
                if (!property.Writable)
                    continue;

                // Пытаемся найти в JSON соответствующее свойство
                JToken token;
                if (dataToken is JObject dataObj && dataObj.TryGetValue(property.PropertyName, out token))
                {
                    // Для каждого свойства вызываем десериализацию стандартным способом.
                    // Если для этого свойства назначен свой конвертер (например, для Vector3),
                    // то он будет вызван здесь.
                    object propValue = serializer.Deserialize(token.CreateReader(), property.PropertyType);
                    property.ValueProvider.SetValue(instance, propValue);
                }
            }

            // Если версия объекта изменилась, выполняем миграцию
            string currentVersion = VersionAttribute.GetVersion(targetType, "0");
            if (version != currentVersion)
                instance = Migrate(targetType, version, instance);

            return instance;
        }


        public override bool CanConvert(Type objectType)
        {
            // Не обрабатываем примитивы и строки
            if (objectType.IsPrimitive || objectType == typeof(string))
                return false;

            // Исключаем типы списков List<T>
            if (objectType.IsGenericType && objectType.GetGenericTypeDefinition() == typeof(List<>))
                return false;

            return objectType == typeof(object) || (VersionAttribute.GetVersion(objectType, "0") != "0" ||
                                                    DerivedVersionControlAttribute.IsDerivedVersionControl(objectType));
        }

        private static object Migrate(Type type, string version, object data)
        {
            var migrateMethod = type.GetMethod("Migrate", BindingFlags.Static | BindingFlags.Public);
            if (migrateMethod != null)
            {
                return migrateMethod.Invoke(null, new object[] { version, data });
            }

            Console.Error("SerializationVersionControl",
                $"Type {type} does not have a migration method for version {version}");

            return default;
        }
    }
}