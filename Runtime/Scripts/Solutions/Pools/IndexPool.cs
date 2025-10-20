using System;
using System.Collections.Generic;

namespace Metamatic
{
    /// <summary>
    /// Позволяет получить не занятый int index. Удобно для генерации уникальных int идентификаторов.
    /// </summary>
    public class IndexPool
    {
        private const int MaxArrayLength = 0x7FEFFFFF; // ~2,147,483,647 - overhead
        
        public static readonly IndexPool Shared = new IndexPool(1024);

        // Стек освобождённых индексов
        private readonly Stack<int> m_FreeIndices;

        // Текущее фактическое число позиций (верхняя граница).
        private int m_Count;

        /// <summary>
        /// Создаёт пул с начальным размером массива.
        /// </summary>
        /// <param name="initialCapacity">Начальный размер внутреннего массива.</param>
        /// <exception cref="ArgumentOutOfRangeException"/>
        public IndexPool(int initialCapacity)
        {
            if (initialCapacity < 0 || initialCapacity > MaxArrayLength)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity));

            m_FreeIndices = new Stack<int>(initialCapacity);
        }

        /// <summary>
        /// Получить свободный индекс
        /// </summary>
        public int Get()
        {
            // Если есть ранее освобождённые индексы, берём один из них
            if (m_FreeIndices.Count > 0)
            {
                int freeIndex = m_FreeIndices.Pop();
                return freeIndex;
            }

            int newIndex = m_Count;
            m_Count++;
            return newIndex;
        }

        /// <summary>
        /// Удалить элемент по индексу.
        /// </summary>
        /// <param name="index">Индекс, который нужно освободить.</param>
        /// <exception cref="ArgumentOutOfRangeException">Индекс некорректный.</exception>
        /// <exception cref="InvalidOperationException">Ячейка уже свободна.</exception>
        public bool Release(int index)
        {
            if (index < 0 || index >= m_Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            m_FreeIndices.Push(index);

            return false;
        }
    }
}