using System;

namespace Metamatic.SaveSystem
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;

    public class IgnoreTypeReferenceLoopContractResolver : DefaultContractResolver
    {
        protected override JsonContract CreateContract(Type objectType)
        {
            // Создаем "контракт" стандартным способом:
            JsonContract contract = base.CreateContract(objectType);

            // Если это именно System.Type, то отключим/изменим ReferenceLoopHandling
            if (objectType == typeof(Type))
            {
                // 1) можно просто сказать "не хранить ссылку" (IsReference = false),
                //    чтобы JSON.NET не думал, что этот объект надо проверять в стеке:
                contract.IsReference = false;

                // 2) а также установить ReferenceLoopHandling:
                //    - Ignore: JSON.NET в случае "цикличности" просто пропустит свойство
                //    - Serialize: будет пытаться сериализовать дальше (но у вас есть свой конвертер)

                // Если хотите, чтобы вообще вело себя как "примитив" (условно говоря, как string),
                // можно установить кастомный конвертер прямо тут:
                // contract.Converter = new TypeConverter(...);
            }

            return contract;
        }
    }

}