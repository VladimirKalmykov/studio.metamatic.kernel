namespace Metamatic
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    [Serializable]
    public class SerializableDictionary<TKey, TValue> : ISerializationCallbackReceiver
    {
        [SerializeField]
        private List<TKey> keys = new List<TKey>();
    
        [SerializeField]
        private List<TValue> values = new List<TValue>();

        // Словарь для использования во время выполнения

        // Доступ к словарю
        public Dictionary<TKey, TValue> dictionary { get; private set; } = new Dictionary<TKey, TValue>();

        // Реализация интерфейса для сериализации
        public void OnBeforeSerialize()
        {
            keys.Clear();
            values.Clear();
            foreach (var kvp in dictionary)
            {
                keys.Add(kvp.Key);
                values.Add(kvp.Value);
            }
        }

        public void OnAfterDeserialize()
        {
            dictionary = new Dictionary<TKey, TValue>();

            if (keys.Count != values.Count)
                throw new Exception("Ошибка десериализации: количество ключей и значений не совпадает.");

            for (int i = 0; i < keys.Count; i++)
            {
                dictionary.Add(keys[i], values[i]);
            }
        }

        // Методы для работы со словарём
        public void Add(TKey key, TValue value)
        {
            dictionary.Add(key, value);
        }

        public bool ContainsKey(TKey key)
        {
            return dictionary.ContainsKey(key);
        }

        public TValue this[TKey key]
        {
            get => dictionary[key];
            set => dictionary[key] = value;
        }
        
        public void Clear()
        {
            dictionary.Clear();
        }
    }

}