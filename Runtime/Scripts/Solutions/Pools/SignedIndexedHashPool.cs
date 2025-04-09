using Cysharp.Threading.Tasks;

namespace Metamatic
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Most heaviest variant of SignedIndexedPool that allows to get items by their instance, subscribe to events and remove items by instance.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class SignedIndexedHashPool<T>
    {
        private const int MaxArrayLength = 0x7FEFFFFF; // ~2,147,483,647 - overhead

        // В одном массиве храним пары (Value, Used).
        private (T Value, bool Used, long Signature)[] m_Items;
        

        // Стек освобождённых индексов
        private readonly Stack<int> m_FreeIndices;
        
        // Стек занятых интексов (для быстрого GetEnumerator)
        private readonly List<int> m_UsedIndexes;
        
        // HashSet значений (для быстрого Contains)
        private readonly HashSet<T> m_ItemsSet;

        private long m_LastSignature;

        // Текущее фактическое число позиций (верхняя граница).
        private int m_Count;
        
        public event Action<int, T> OnAdd;
        public event Action<int, T> OnRemove;

        /// <summary>
        /// Создаёт пул с начальным размером массива.
        /// </summary>
        /// <param name="initialCapacity">Начальный размер внутреннего массива.</param>
        /// <exception cref="ArgumentOutOfRangeException"/>
        public SignedIndexedHashPool(int initialCapacity)
        {
            if (initialCapacity < 0 || initialCapacity > MaxArrayLength)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity));

            m_Items = new (T, bool, long)[initialCapacity];
            m_FreeIndices = new Stack<int>(initialCapacity);
            m_UsedIndexes = new List<int>(initialCapacity);
            m_ItemsSet = new HashSet<T>();
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
                m_UsedIndexes.Add(freeIndex);
                m_ItemsSet.Add(item);
                var freeIndexToken = new Token96Bit(freeIndex, m_LastSignature);
                
                OnAdd?.Invoke(freeIndex, item);
                return freeIndexToken;
            }

            // Если нет свободных индексов — используем следующий индекс в массиве
            if (m_Count >= m_Items.Length)
                ResizeArray();

            int newIndex = m_Count;
            m_Items[newIndex] = (item, true, m_LastSignature);
            m_UsedIndexes.Add(newIndex);
            m_ItemsSet.Add(item);
            m_Count++;
            var token = new Token96Bit(newIndex, m_LastSignature);
            OnAdd?.Invoke(newIndex, item);
            return token;
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

            ref var item = ref m_Items[index];
            var value = item.Value;
            if (item.Signature == signature)
            {
                m_Items[index] = (default!, false, 0);
                m_FreeIndices.Push(index);
                m_UsedIndexes.Remove(index);
                m_ItemsSet.Remove(value);
                OnRemove?.Invoke(index, value);
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
            if (index < 0 || index >= m_Count)
            {
                item = default!;
                return false;
            }
            
            ref var tmpItem = ref m_Items[index];
            if (!tmpItem.Used || tmpItem.Signature != signature)
            {
                item = default!;
                return false;
            }
            

            item = tmpItem.Value;
            m_Items[index] = (default!, false, 0);
            m_UsedIndexes.Remove(index);
            m_ItemsSet.Remove(item);
            m_FreeIndices.Push(index);
            OnRemove?.Invoke(index, item);
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
        
        public bool Contains(T instance)
        {
            return m_ItemsSet.Contains(instance);
        }

        /// <summary>
        /// Watch every item async.
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public DisposableAction WatchEveryAsync(Func<T, Signal, UniTaskVoid> action)
        {
            var listeners = ParamLessPool<Dictionary<int, SignalSource>>.GetShared();
            foreach (var index in m_UsedIndexes)
            {
                var css = SignalSource.Create();
                action(m_Items[index].Value, css.Signal).Forget();
                listeners[index] = css;
            }

            OnRemove += RemoveHandler;
            OnAdd += AddHandler;

            return DisposableAction.Create(() =>
            {
                OnRemove -= RemoveHandler;
                OnAdd -= AddHandler;
                
                foreach (var css in listeners)
                {
                    css.Value.TryRaise();
                }
                
                listeners.Clear();
                ParamLessPool<Dictionary<int, SignalSource>>.ReturnShared(listeners);
            });
            
            void AddHandler(int index, T value)
            {
                var css = SignalSource.Create();
                action(value, css.Signal).Forget();
                listeners[index] = css;
            }
            
            void RemoveHandler(int index, T value)
            {
                if (listeners.TryGetValue(index, out var css))
                {
                    css.TryRaise();
                }
            }
        }

        /// <summary>
        /// Текущее число занятых элементов в пуле.
        /// </summary>
        /// <remarks>
        /// Т. е. общее число позиций (m_Count) минус количество освобождённых индексов.
        /// </remarks>
        // ReSharper disable once InconsistentNaming
        public int Count => m_ItemsSet.Count;
        
        public IEnumerator<T> GetEnumerator()
        {
            foreach (var index in m_UsedIndexes)
            {
                yield return m_Items[index].Value;
            }
        }
        
        public T[] ToArray()
        {
            var array = new T[m_ItemsSet.Count];
            int i = 0;
            foreach (var item in this)
            {
                array[i] = item;
                i++;
            }

            return array;
        }
        
        public bool Dequeue(out T item)
        {
            if (m_UsedIndexes.Count == 0)
            {
                item = default!;
                return false;
            }

            var index = m_UsedIndexes[0];
            item = m_Items[index].Value;
            Remove(index, m_Items[index].Signature);
            return true;
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
