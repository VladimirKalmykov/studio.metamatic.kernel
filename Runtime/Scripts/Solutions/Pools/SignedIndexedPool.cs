namespace Metamatic
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Powerful root that allows to store items in a pool and get/remove them by index.
    /// Single-threaded version of this pool gives x20 performance boost (~100,000 iterations per 1ms)
    /// compared to a multi-threaded version with locking.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class SignedIndexedPool<T>
    {
        private const int MaxArrayLength = 0x7FEFFFFF; // ~2,147,483,647 - overhead

        // В одном массиве храним пары (Value, Used).
        private (T Value, bool Used, long Signature)[] m_Items;

        // Стек освобождённых индексов
        private readonly Stack<int> m_FreeIndices;

        private long m_LastSignature;

        // Текущее фактическое число позиций (верхняя граница).
        private int m_Count;

        /// <summary>
        /// Создаёт пул с начальным размером массива.
        /// </summary>
        /// <param name="initialCapacity">Начальный размер внутреннего массива.</param>
        /// <exception cref="ArgumentOutOfRangeException"/>
        public SignedIndexedPool(int initialCapacity)
        {
            if (initialCapacity < 0 || initialCapacity > MaxArrayLength)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity));

            m_Items = new (T, bool, long)[initialCapacity];
            m_FreeIndices = new Stack<int>(initialCapacity);
        }

        /// <summary>
        /// Добавить элемент в пул и вернуть его индекс.
        /// </summary>
        /// <param name="item">Элемент, который хотим поместить в пул.</param>
        /// <returns>Индекс-идентификатор этого элемента.</returns>
        public Token96Bit Add(T item)
        {
            unchecked
            {
                m_LastSignature++;
            }
            // Если есть ранее освобождённые индексы, берём один из них
            if (m_FreeIndices.Count > 0)
            {
                int freeIndex = m_FreeIndices.Pop();
                m_Items[freeIndex] = (item, true, m_LastSignature);
                return new Token96Bit(freeIndex, m_LastSignature);
            }

            // Если нет свободных индексов — используем следующий индекс в массиве
            if (m_Count >= m_Items.Length)
                ResizeArray();

            int newIndex = m_Count;
            m_Items[newIndex] = (item, true, m_LastSignature);
            m_Count++;
            return new Token96Bit(newIndex, m_LastSignature);
        }

        /// <summary>
        /// Удалить элемент по индексу.
        /// </summary>
        /// <param name="index">Индекс, который нужно освободить.</param>
        /// <exception cref="ArgumentOutOfRangeException">Индекс некорректный.</exception>
        /// <exception cref="InvalidOperationException">Ячейка уже свободна.</exception>
        public bool Remove(int index, long signature)
        {
            if (index < 0 || index >= m_Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            // Проверяем, не уже ли свободен индекс
            if (!m_Items[index].Used)
            {
                return false;
            }

            if (m_Items[index].Signature == signature)
            {
                m_Items[index] = (default!, false, 0);
                m_FreeIndices.Push(index);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Удалить элемент по индексу с возвращением удалённого элемента.
        /// </summary>
        /// <param name="index">Индекс, который нужно освободить.</param>
        /// <param name="item">Значение, находившееся в пуле по этому индексу.</param>
        /// <returns>true, если удаление прошло успешно; иначе false.</returns>
        public bool Remove(int index, long signature, out T item)
        {
            if (index < 0 || index >= m_Count || !m_Items[index].Used || m_Items[index].Signature != signature)
            {
                item = default!;
                return false;
            }

            item = m_Items[index].Value;
            m_Items[index] = (default!, false, 0);
            m_FreeIndices.Push(index);
            return true;
        }
        
        public bool Remove(Token96Bit token)
        {
            return Remove(token.ID, token.Signature);
        }
        
        public bool Remove(Token96Bit token, out T item)
        {
            return Remove(token.ID, token.Signature, out item);
        }

        /// <summary>
        /// Получить элемент по индексу (с выбросом исключения, если он не валиден).
        /// </summary>
        /// <param name="index">Индекс в пуле.</param>
        /// <returns>Элемент, если индекс верный и занятый.</returns>
        public T Get(int index)
        {
            if (index < 0 || index >= m_Count || !m_Items[index].Used)
                throw new ArgumentOutOfRangeException(nameof(index),
                    "Index is out of range or not used.");

            return m_Items[index].Value;
        }

        /// <summary>
        /// Получить элемент по индексу (с выбросом исключения, если он не валиден).
        /// </summary>
        /// <param name="index">Индекс в пуле.</param>
        /// <param name="signature"></param>
        /// <returns>Элемент, если индекс верный и занятый.</returns>
        public T Get(int index, long signature)
        {
            if (index < 0 || index >= m_Count || !m_Items[index].Used || m_Items[index].Signature != signature)
                throw new ArgumentOutOfRangeException(nameof(index),
                    "Index is out of range or not used. Or signature is invalid.");

            return m_Items[index].Value;
        }

        /// <summary>
        /// Безопасно получить элемент по индексу (возвращает false, если индекс некорректный или ячейка свободна).
        /// </summary>
        public bool TryGet(int index, long signature, out T item)
        {
            if (index < 0 || index >= m_Count)
            {
                item = default!;
                return false;
            }
            var itemData = m_Items[index];
            if (!itemData.Used || itemData.Signature != signature)
            {
                item = default!;
                return false;
            }

            item = itemData.Value;
            return true;
        }
        
        public bool TryGet(Token96Bit token, out T item)
        {
            return TryGet(token.ID, token.Signature, out item);
        }

        /// <summary>
        /// Проверить, «занят» ли индекс (true, если там лежит элемент).
        /// </summary>
        public bool Has(int index)
        {
            if (index < 0 || index >= m_Count)
                return false;
            return m_Items[index].Used;
        }

        /// <summary>
        /// Текущее число занятых элементов в пуле.
        /// </summary>
        /// <remarks>
        /// Т. е. общее число позиций (m_Count) минус количество освобождённых индексов.
        /// </remarks>
        public int Count => m_Count - m_FreeIndices.Count;
        
        [Obsolete("Unstable", true)]
        public IEnumerator<T> GetEnumerator()
        {
            // Проходим по всем позициям от 0 до m_Count (верхняя граница)
            for (int i = 0; i < m_Count; i++)
            {
                // Возвращаем только те элементы, которые используются
                if (m_Items[i].Used)
                {
                    yield return m_Items[i].Value;
                }
            }
        }
        
        [Obsolete("Unstable", true)]
        public T[] ToArray()
        {
            T[] result = new T[Count];
            int index = 0;
            for (int i = 0; i < m_Count; i++)
            {
                if (m_Items[i].Used)
                {
                    result[index] = m_Items[i].Value;
                    index++;
                }
            }
            return result;
        }

        public void Clear()
        {
            Array.Clear(m_Items, 0, m_Count);
            m_FreeIndices.Clear();
            m_Count = 0;
            m_LastSignature = 0;
        }

        /// <summary>
        /// Увеличивает размер внутреннего массива в 2 раза (пока не достигнем MaxArrayLength).
        /// </summary>
        private void ResizeArray()
        {
            #if DEBUG
            Console.Info("SignedIndexedPool", $"Resizing array from {m_Items.Length} to {m_Items.Length * 2}");
            #endif
            if (m_Items.Length == MaxArrayLength)
                throw new InvalidOperationException("Cannot exceed maximum array size.");

            long newLength = (long)m_Items.Length * 2;
            if (newLength > MaxArrayLength)
                newLength = MaxArrayLength;

            Array.Resize(ref m_Items, (int)newLength);
        }
    }
}
