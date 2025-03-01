using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Metamatic.SaveSystem
{
    public static class OrchardSerializationUtils
    {
        /// <summary>
        /// Сериализует объект <paramref name="data"/> в JSON, используя <see cref="SerializationVersionControlConverter"/>.
        /// В процессе сериализации использует списки <paramref name="types"/> и <paramref name="glossary"/>.
        /// </summary>
        /// <param name="data">Объект, который нужно сериализовать</param>
        /// <param name="types">Список Type, куда будут дописываться новые типы, встречающиеся при сериализации</param>
        /// <param name="glossary">Список версий (строк), куда будут дописываться новые версии, встречающиеся при сериализации</param>
        /// <returns>Строка JSON</returns>
        public static string SerializeObjectWithVersionControl(
            object data,
            List<Type> types,
            List<string> glossary)
        {
            // 1) Создаём экземпляр конвертера, передавая наши списки
            var versionControlConverter = new SerializationVersionControlConverter(types, glossary);
            var typeConverter = new TypeConverter(types);
            var assetReferenceConverter = new AssetReferenceJsonConverter(types, glossary);
            var vector2Converter = new Vector2JsonConverter();
            var vector3Converter = new Vector3JsonConverter();
            var quaternionConverter = new QuaternionJsonConverter();

            // Resolvers
            
            // 2) Настраиваем JsonSerializerSettings
            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                ContractResolver = new IgnoreTypeReferenceLoopContractResolver(),
                // Можно убрать/изменить на None, если не хотите \"красивый\" JSON
                Converters = new List<JsonConverter>
                {
                    versionControlConverter,
                    typeConverter,
                    assetReferenceConverter,
                    vector2Converter,
                    vector3Converter,
                    quaternionConverter
                }
            };

            // 3) Вызываем сериализацию
            string json = JsonConvert.SerializeObject(data, settings);
            return json;
        }

        /// <summary>
        /// Десериализует JSON, используя SerializationVersionControlConverter, и возвращает объект типа T.
        /// </summary>
        /// <param name="json">Строка JSON</param>
        /// <param name="types">Список Type, ранее заполненный (например, извлечённый из сохранения)</param>
        /// <param name="glossary">Список версий (строк), ранее заполненный</param>
        /// <typeparam name="T">Тип, к которому нужно привести десериализованный объект</typeparam>
        /// <returns>Экземпляр T</returns>
        public static T DeserializeObjectWithVersionControl<T>(
            string json,
            List<Type> types,
            List<string> glossary)
        {
            // 1) Создаём конвертер
            var versionControlConverter = new SerializationVersionControlConverter(types, glossary);
            var typeConverter = new TypeConverter(types);
            var assetReferenceConverter = new AssetReferenceJsonConverter(types, glossary);
            var vector2Converter = new Vector2JsonConverter();
            var vector3Converter = new Vector3JsonConverter();
            var quaternionConverter = new QuaternionJsonConverter();

            // 2) Настраиваем JsonSerializerSettings
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new IgnoreTypeReferenceLoopContractResolver(),
                Converters = new List<JsonConverter>
                {
                    versionControlConverter,
                    typeConverter,
                    assetReferenceConverter,
                    vector2Converter,
                    vector3Converter,
                    quaternionConverter
                }
                // Можно добавить и другие настройки: TypeNameHandling, MissingMemberHandling и т. п.
            };

            // 3) Выполняем десериализацию
            T result = JsonConvert.DeserializeObject<T>(json, settings);
            return result;
        }

        /// <summary>
        /// Преобразует список типов в массив строк, где каждая строка - AssemblyQualifiedName.
        /// </summary>
        /// <param name="types">Список типов</param>
        /// <returns>Массив строк с AssemblyQualifiedName</returns>
        public static string[] ConvertTypesToAssemblyQualifiedNames(List<Type> types)
        {
            if (types == null) return Array.Empty<string>();

            var result = new string[types.Count];
            for (int i = 0; i < types.Count; i++)
            {
                Type t = types[i];
                // Если t == null, можно обработать отдельно, но предположим, что там нет null
                result[i] = t.AssemblyQualifiedName;
            }

            return result;
        }

        public static string PackOrchardV1(List<Type> types, List<string> glossary, string data)
        {
            var typesJson = JsonConvert.SerializeObject(ConvertTypesToAssemblyQualifiedNames(types));
            var glossaryJson = JsonConvert.SerializeObject(glossary);
            
            return $"v.1\n--\n{typesJson}\n--\n{glossaryJson}\n--\n{data}";
        }
        
        public static (List<Type> types, List<string> glossary, string data) UnpackOrchardV1(string packedData)
        {
            var parts = packedData.Split(new[] {"\n--\n"}, StringSplitOptions.None);
            if (parts.Length != 4)
            {
                throw new ArgumentException("Invalid packed data");
            }

            var types = JsonConvert.DeserializeObject<List<string>>(parts[1]);
            var glossary = JsonConvert.DeserializeObject<List<string>>(parts[2]);
            var data = parts[3];
            
            var typesList = new List<Type>();
            foreach (var typeName in types)
            {
                var type = Type.GetType(typeName);
                if (type != null)
                {
                    typesList.Add(type);
                }
            }

            return (typesList, glossary, data);
        }

        /// <summary>
        /// Serialize using NewtonSoft.Json with default settings
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string PrimitiveSerialize(object data)
        {
            return JsonConvert.SerializeObject(data);
        }
        
        /// <summary>
        /// Deserialize using NewtonSoft.Json with default settings
        /// </summary>
        /// <param name="json"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T PrimitiveDeserialize<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json);
        }
    }
}