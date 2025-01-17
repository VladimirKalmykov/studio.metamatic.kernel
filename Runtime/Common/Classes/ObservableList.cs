using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    public class ObservableList<T> : IList<T>, IDisposable
    {
        private readonly List<T> m_List = new List<T>();
        private Func<T, Signal, UniTask> m_Handler;
        private readonly Dictionary<T, SignalSource> m_CancellationTokens = new Dictionary<T, SignalSource>();
        private bool m_Disposed = false;
        public event Action<T, int, Signal> OnItemAdded;
        public IReadOnlyCollection<T> items => m_List;

        public ObservableList(Func<T, Signal, UniTask> handler = null)
        {
            m_Handler = handler ?? DefaultHandler;
        }
        
        public void SetHandler(Func<T, Signal, UniTask> handler)
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(ObservableList<T>));

            m_Handler = handler ?? DefaultHandler;
        }
        
        public DisposableAction WatchEach(Action<T, int> handler, Func<T, int, bool> filter = null)
        {
            void ForgetHandler(T item, int index, Signal signal)
            {
                if (filter != null && !filter(item, index))
                    return;
                handler(item, index);
            }
            for (var i = 0; i < m_List.Count; i++)
            {
                var item = m_List[i];
                if (filter != null && !filter(item, i))
                    continue;
                ForgetHandler(item, i, m_CancellationTokens[item].Signal);
            }
            OnItemAdded += ForgetHandler;
            return new DisposableAction(() =>
            {
                OnItemAdded -= ForgetHandler;
            });
        }
        
        public DisposableAction WatchEachLifecycle(Func<T, int, Signal, UniTaskVoid> handler, Func<T, int, bool> filter = null)
        {
            var cssList = new List<SignalSource>();
            void ForgetAsyncHandler(T item, int index, Signal signal)
            {
                if (filter != null && !filter(item, index))
                    return;
                var css = SignalSource.CreateLinked(signal);
                cssList.Add(css);
                handler(item, index, css.Signal).Forget();
            }
            /*
             * First, iterate over existing items and call the handler for each item.
             */
            for (var i = 0; i < m_List.Count; i++)
            {
                var item = m_List[i];
                if (filter != null && !filter(item, i))
                    continue;
                ForgetAsyncHandler(item, i, m_CancellationTokens[item].Signal);
            }
            OnItemAdded += ForgetAsyncHandler;
            return new DisposableAction(() =>
            {
                cssList.ForEach(css => css.TryRaise());
                OnItemAdded -= ForgetAsyncHandler;
            });
        }
        
        private static UniTask DefaultHandler(T item, Signal signal)
        {
            return UniTask.CompletedTask;
        }
        
        /// <summary>
        /// Syncs the list with the new items. Removes items that are not in the new list and adds that are not in this list.
        /// </summary>
        /// <param name="items"></param>
        public void Synchronize(IEnumerable<T> items)
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
                throw new ObjectDisposedException(nameof(ObservableList<T>));
            if (m_Handler == null)
                m_Handler = DefaultHandler;

            var cts = SignalSource.Create();
            var signal = cts.Signal;
            m_CancellationTokens[item] = cts;
            m_List.Add(item);
            OnItemAdded?.Invoke(item, m_List.Count - 1, signal);

            try
            {
                await m_Handler(item, cts.Signal);
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
                throw new ObjectDisposedException(nameof(ObservableList<T>));
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            var cts = SignalSource.Create();
            m_CancellationTokens[item] = cts;
            m_List.Add(item);
            
            var signal = cts.Signal;
            m_Handler(item, signal).Forget();
            OnItemAdded?.Invoke(item, m_List.Count - 1, signal);
        }
        
        /// <summary>
        /// Add item and get a disposable action to remove it when it's no longer needed.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public DisposableAction AddManaged(T item)
        {
            Add(item);
            return new DisposableAction(() =>
            {
                Remove(item);
            });
        }
        
        public void TryAdd(T item)
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(ObservableList<T>));
            if (m_List.Contains(item))
                return;

            var cts = SignalSource.Create();
            m_CancellationTokens[item] = cts;
            m_List.Add(item);
            
            var signal = cts.Signal;
            m_Handler(item, signal).Forget();
            OnItemAdded?.Invoke(item, m_List.Count - 1, signal);
        }

        public bool Remove(T item)
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(ObservableList<T>));
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            if (m_List.Remove(item))
            {
                if (m_CancellationTokens.TryGetValue(item, out var cts))
                {
                    cts.TryRaise();
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
                cts.TryRaise();
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
                    throw new ObjectDisposedException(nameof(ObservableList<T>));

                if (m_CancellationTokens.TryGetValue(m_List[index], out var cts))
                {
                    cts.TryRaise();
                    m_CancellationTokens.Remove(m_List[index]);
                }

                m_List[index] = value;

                cts = SignalSource.Create();
                m_CancellationTokens[value] = cts;

                var signal = cts.Signal;
                m_Handler(value, signal).Forget();
                OnItemAdded?.Invoke(value, index, signal);
            }
        }

        public int IndexOf(T item) => m_List.IndexOf(item);

        public void Insert(int index, T item)
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(ObservableList<T>));
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            if (index < m_List.Count)
            {
                var existingItem = m_List[index];
                if (m_CancellationTokens.TryGetValue(existingItem, out var cts))
                {
                    cts.TryRaise();
                    m_CancellationTokens.Remove(existingItem);
                }
            }

            m_List.Insert(index, item);

            var newCts = SignalSource.Create();
            m_CancellationTokens[item] = newCts;
            
            var signal = newCts.Signal;
            m_Handler(item, signal).Forget();
            OnItemAdded?.Invoke(item, index, signal);
        }

        public void RemoveAt(int index)
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(ObservableList<T>));

            var item = m_List[index];
            m_List.RemoveAt(index);

            if (m_CancellationTokens.TryGetValue(item, out var cts))
            {
                cts.TryRaise();
                m_CancellationTokens.Remove(item);
            }
        }

        public void Clear()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(ObservableList<T>));

            foreach (var cts in m_CancellationTokens.Values)
            {
                cts.TryRaise();
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
