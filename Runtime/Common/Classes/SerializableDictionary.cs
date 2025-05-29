namespace Metamatic
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    [Serializable]
    public class SerializableDictionary<TKey, TValue> : ISerializationCallbackReceiver, IEnumerable<KeyValuePair<TKey, TValue>>
    {
        [SerializeField]
        private List<TKey> keys = new List<TKey>();
    
        [SerializeField]
        private List<TValue> values = new List<TValue>();

        // Словарь для использования во время выполнения
        public Dictionary<TKey, TValue> dictionary { get; private set; } = new Dictionary<TKey, TValue>();

        // Количество элементов
        public int Count => keys.Count;

        // Доступ к словарю по ключу
        public TValue this[TKey key]
        {
            get => dictionary[key];
            set => dictionary[key] = value;
        }

        // Доступ к паре ключ-значение по индексу
        public KeyValuePair<TKey, TValue> this[int index]
        {
            get
            {
                if (index < 0 || index >= keys.Count)
                    throw new ArgumentOutOfRangeException(nameof(index));
                return new KeyValuePair<TKey, TValue>(keys[index], values[index]);
            }
            set
            {
                if (index < 0 || index >= keys.Count)
                    throw new ArgumentOutOfRangeException(nameof(index));

                // Удаляем старую пару из словаря
                var oldKey = keys[index];
                dictionary.Remove(oldKey);

                // Обновляем списки ключей и значений
                keys[index] = value.Key;
                values[index] = value.Value;

                // Добавляем новую пару в словарь
                dictionary.Add(value.Key, value.Value);
            }
        }

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

        public void Clear()
        {
            dictionary.Clear();
        }

        // Реализация перечисления
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return dictionary.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
