namespace Metamatic
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    /// <summary>
    /// Allows to store items in a pool and get them by index. Thread-safe version of this pool takes x20 performance compared to IndexedPool,
    /// this try to use it only when you need thread-safety.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class ConcurrentIndexedPool<T> : IDisposable
    {
        private const int MaxArrayLength = 0x7FEFFFFF; // ~2,147,483,647 - overhead
        private SpinLock m_Lock = new SpinLock();
        private T[] m_Items;
        private readonly Stack<int> _freeIndices;
        private int m_Count;
        private bool m_Disposed;

        /// <summary>
        /// Создаёт пул с начальным размером массива.
        /// </summary>
        /// <param name="initialCapacity">Желаемый начальный размер массива.</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public ConcurrentIndexedPool(int initialCapacity)
        {
            if (initialCapacity < 0 || initialCapacity > MaxArrayLength)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity));

            m_Items = new T[initialCapacity];
            _freeIndices = new Stack<int>(capacity: initialCapacity);
        }

        /// <summary>
        /// Add element to the pool and return its index.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public int Add(T item)
        {
            CheckDisposed();
            bool lockTaken = false;

            try
            {
                m_Lock.Enter(ref lockTaken);

                // Пытаемся взять индекс из стека свободных
                if (_freeIndices.Count > 0)
                {
                    int freeIndex = _freeIndices.Pop();
                    m_Items[freeIndex] = item;
                    return freeIndex;
                }

                // Если свободного индекса нет - идём в конец
                if (m_Count >= m_Items.Length)
                {
                    // Нужно расширяться
                    ResizeArray();
                }

                int newIndex = m_Count;
                m_Items[newIndex] = item;
                m_Count++;
                return newIndex;
            }
            finally
            {
                if (lockTaken) m_Lock.Exit();
            }
        }

        /// <summary>
        /// Remove element by index.
        /// </summary>
        /// <param name="index"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void Remove(int index)
        {
            CheckDisposed();
            bool lockTaken = false;

            try
            {
                m_Lock.Enter(ref lockTaken);

                // Доп. проверки (необязательно, но полезно)
                if (index < 0 || index >= m_Count)
                    throw new ArgumentOutOfRangeException(nameof(index));

                m_Items[index] = default!;
                _freeIndices.Push(index);
            }
            finally
            {
                if (lockTaken) m_Lock.Exit();
            }
        }

        /// <summary>
        /// Get element by index.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public T Get(int index)
        {
            CheckDisposed();

            // Если нужно полная потокобезопасность - обёртывайте в lock.
            // Если только чтение - можно обходиться без lock при условии, 
            // что не будет параллельного Remove для того же индекса.

            if (index < 0 || index >= m_Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            return m_Items[index];
        }

        /// <summary>
        /// Занимает ли пул сейчас какие-то элементы?
        /// </summary>
        public int Count => m_Count - _freeIndices.Count;

        /// <summary>
        /// Реализация IDisposable. Очищаемся, чтобы дальнейший доступ был невозможен.
        /// </summary>
        public void Dispose()
        {
            bool lockTaken = false;
            try
            {
                m_Lock.Enter(ref lockTaken);

                if (!m_Disposed)
                {
                    m_Disposed = true;
                    // Если здесь нужно подчистить что-то в T, 
                    // можно перебрать все элементы и вызвать их Dispose() (если T : IDisposable).
                    // Пока ограничиваемся сбросом ссылок.
                    m_Items = Array.Empty<T>();
                    _freeIndices.Clear();
                }
            }
            finally
            {
                if (lockTaken) m_Lock.Exit();
            }
        }

        private void ResizeArray()
        {
            if (m_Items.Length == MaxArrayLength)
            {
                // Дальше расти уже нельзя
                throw new InvalidOperationException("Cannot exceed maximum array size.");
            }

            long newLength = (long)m_Items.Length * 2;
            if (newLength > MaxArrayLength)
                newLength = MaxArrayLength;

            Array.Resize(ref m_Items, (int)newLength);
        }

        private void CheckDisposed()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(ConcurrentIndexedPool<T>));
        }
    }

}