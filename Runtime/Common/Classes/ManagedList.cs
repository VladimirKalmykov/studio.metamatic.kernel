using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading;

    public class ManagedList<T> : IList<T>, IDisposable
    {
        private readonly List<T> m_List = new List<T>();
        private Func<T, CancellationToken, UniTask> m_Handler;
        private readonly Dictionary<T, CancellationTokenSource> m_CancellationTokens = new Dictionary<T, CancellationTokenSource>();
        private bool m_Disposed = false;

        public ManagedList(Func<T, CancellationToken, UniTask> handler)
        {
            m_Handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }
        
        public void SetHandler(Func<T, CancellationToken, UniTask> handler)
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(ManagedList<T>));

            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            m_Handler = handler;
        }
        
        private static UniTask DefaultHandler(T item, CancellationToken token)
        {
            return UniTask.CompletedTask;
        }
        
        /// <summary>
        /// Syncs the list with the new items. Removes items that are not in the new list and adds that are not in this list.
        /// </summary>
        /// <param name="items"></param>
        public void Synchronize(T[] items)
        {
            /* Remove items that are not in the new list and add that are not in this list */
            for (var i = m_List.Count - 1; i >= 0; i--)
            {
                var item = m_List[i];
                if (!items.Contains(item))
                {
                    Remove(item);
                }
            }
            foreach (var item in items)
            {
                TryAdd(item);
            }
        }
        
        public void Synchronize(List<T> items)
        {
            for (var i = m_List.Count - 1; i >= 0; i--)
            {
                var item = m_List[i];
                if (!items.Contains(item))
                {
                    Remove(item);
                }
            }
            foreach (var item in items)
            {
                TryAdd(item);
            }
        }

        public async UniTask AddAsync(T item)
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(ManagedList<T>));

            var cts = new CancellationTokenSource();
            m_CancellationTokens[item] = cts;
            m_List.Add(item);

            try
            {
                await m_Handler(item, cts.Token);
            }
            catch (OperationCanceledException)
            {
                // Handle the cancellation if needed
            }
            catch (Exception e)
            {
                // Handle the exception if needed
                Debug.LogError(e);
            }
        }

        public void Add(T item)
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(ManagedList<T>));

            var cts = new CancellationTokenSource();
            m_CancellationTokens[item] = cts;
            m_List.Add(item);

            m_Handler(item, cts.Token).Forget();
        }
        
        public void TryAdd(T item)
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(ManagedList<T>));
            if (m_List.Contains(item))
                return;

            var cts = new CancellationTokenSource();
            m_CancellationTokens[item] = cts;
            m_List.Add(item);

            m_Handler(item, cts.Token).Forget();
        }

        public bool Remove(T item)
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(ManagedList<T>));

            if (m_List.Remove(item))
            {
                if (m_CancellationTokens.TryGetValue(item, out var cts))
                {
                    cts.Cancel();
                    cts.Dispose();
                    m_CancellationTokens.Remove(item);
                }

                return true;
            }

            return false;
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;

            foreach (var cts in m_CancellationTokens.Values)
            {
                cts.Cancel();
                cts.Dispose();
            }
            m_CancellationTokens.Clear();
            m_Disposed = true;
        }

        #region IList<T> Implementation

        public int Count => m_List.Count;

        public bool IsReadOnly => false;

        public T this[int index]
        {
            get => m_List[index];
            set
            {
                if (m_Disposed)
                    throw new ObjectDisposedException(nameof(ManagedList<T>));

                if (m_CancellationTokens.TryGetValue(m_List[index], out var cts))
                {
                    cts.Cancel();
                    m_CancellationTokens.Remove(m_List[index]);
                }

                m_List[index] = value;

                cts = new CancellationTokenSource();
                m_CancellationTokens[value] = cts;

                m_Handler(value, cts.Token).Forget();
            }
        }

        public int IndexOf(T item) => m_List.IndexOf(item);

        public void Insert(int index, T item)
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(ManagedList<T>));

            if (index < m_List.Count)
            {
                var existingItem = m_List[index];
                if (m_CancellationTokens.TryGetValue(existingItem, out var cts))
                {
                    cts.Cancel();
                    m_CancellationTokens.Remove(existingItem);
                }
            }

            m_List.Insert(index, item);

            var newCts = new CancellationTokenSource();
            m_CancellationTokens[item] = newCts;

            m_Handler(item, newCts.Token).Forget();
        }

        public void RemoveAt(int index)
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(ManagedList<T>));

            var item = m_List[index];
            m_List.RemoveAt(index);

            if (m_CancellationTokens.TryGetValue(item, out var cts))
            {
                cts.Cancel();
                m_CancellationTokens.Remove(item);
            }
        }

        public void Clear()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(ManagedList<T>));

            foreach (var cts in m_CancellationTokens.Values)
            {
                cts.Cancel();
            }
            m_CancellationTokens.Clear();
            m_List.Clear();
        }

        public bool Contains(T item) => m_List.Contains(item);

        public void CopyTo(T[] array, int arrayIndex) => m_List.CopyTo(array, arrayIndex);

        public IEnumerator<T> GetEnumerator() => m_List.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion

        #region AddRange Implementation

        public async UniTask AddRangeAsync(IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                await AddAsync(item);
            }
        }

        public void AddRange(IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                Add(item);
            }
        }

        #endregion
    }
}
