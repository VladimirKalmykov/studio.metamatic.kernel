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
    public sealed class IndexedPool<T>
    {
        private const int MaxArrayLength = 0x7FEFFFFF; // ~2,147,483,647 - overhead

        // В одном массиве храним пары (Value, Used).
        private (T Value, bool Used)[] m_Items;

        // Стек освобождённых индексов
        private readonly Stack<int> m_FreeIndices;

        // Текущее фактическое число позиций (верхняя граница).
        private int m_Count;

        /// <summary>
        /// Создаёт пул с начальным размером массива.
        /// </summary>
        /// <param name="initialCapacity">Начальный размер внутреннего массива.</param>
        /// <exception cref="ArgumentOutOfRangeException"/>
        public IndexedPool(int initialCapacity)
        {
            if (initialCapacity < 0 || initialCapacity > MaxArrayLength)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity));

            m_Items = new (T, bool)[initialCapacity];
            m_FreeIndices = new Stack<int>(initialCapacity);
        }

        /// <summary>
        /// Добавить элемент в пул и вернуть его индекс.
        /// </summary>
        /// <param name="item">Элемент, который хотим поместить в пул.</param>
        /// <returns>Индекс-идентификатор этого элемента.</returns>
        public int Add(T item)
        {
            // Если есть ранее освобождённые индексы, берём один из них
            if (m_FreeIndices.Count > 0)
            {
                int freeIndex = m_FreeIndices.Pop();
                m_Items[freeIndex] = (item, true);
                return freeIndex;
            }

            // Если нет свободных индексов — используем следующий индекс в массиве
            if (m_Count >= m_Items.Length)
                ResizeArray();

            int newIndex = m_Count;
            m_Items[newIndex] = (item, true);
            m_Count++;
            return newIndex;
        }

        /// <summary>
        /// Удалить элемент по индексу.
        /// </summary>
        /// <param name="index">Индекс, который нужно освободить.</param>
        /// <exception cref="ArgumentOutOfRangeException">Индекс некорректный.</exception>
        /// <exception cref="InvalidOperationException">Ячейка уже свободна.</exception>
        public void Remove(int index)
        {
            if (index < 0 || index >= m_Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            // Проверяем, не уже ли свободен индекс
            if (!m_Items[index].Used)
                throw new InvalidOperationException($"Index {index} is already free.");

            m_Items[index] = (default!, false);
            m_FreeIndices.Push(index);
        }

        /// <summary>
        /// Удалить элемент по индексу с возвращением удалённого элемента.
        /// </summary>
        /// <param name="index">Индекс, который нужно освободить.</param>
        /// <param name="item">Значение, находившееся в пуле по этому индексу.</param>
        /// <returns>true, если удаление прошло успешно; иначе false.</returns>
        public bool Remove(int index, out T item)
        {
            if (index < 0 || index >= m_Count || !m_Items[index].Used)
            {
                item = default!;
                return false;
            }

            item = m_Items[index].Value;
            m_Items[index] = (default!, false);
            m_FreeIndices.Push(index);
            return true;
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
        /// Безопасно получить элемент по индексу (возвращает false, если индекс некорректный или ячейка свободна).
        /// </summary>
        public bool TryGet(int index, out T item)
        {
            if (index < 0 || index >= m_Count || !m_Items[index].Used)
            {
                item = default!;
                return false;
            }

            item = m_Items[index].Value;
            return true;
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

        /// <summary>
        /// Увеличивает размер внутреннего массива в 2 раза (пока не достигнем MaxArrayLength).
        /// </summary>
        private void ResizeArray()
        {
            if (m_Items.Length == MaxArrayLength)
                throw new InvalidOperationException("Cannot exceed maximum array size.");

            long newLength = (long)m_Items.Length * 2;
            if (newLength > MaxArrayLength)
                newLength = MaxArrayLength;

            Array.Resize(ref m_Items, (int)newLength);
        }
    }
}
