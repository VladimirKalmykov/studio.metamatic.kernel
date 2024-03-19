namespace Metamatic
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    
    /// <summary>
    /// Allows to initialize (or some way process it) items and dispose them when they are not needed anymore.
    /// For argument handler accept item and return disposable object that will be disposed when item is removed from an array passed to Handle method.
    /// </summary>
    /// <example>
    /// var initializer = new ItemsInitializer<ScriptableEffectGameAsset>(asset => asset.Initialize(), cancellationToken);
    /// initializer.OnHandleItem += (asset, disposable) => { Debug.Log($"Handle {asset}"); };
    /// initializer.OnDisposeItem += (asset, disposable) => { Debug.Log($"Dispose {asset}"); };
    /// initializer.OnError += ex => { Debug.LogException(ex); };
    /// initializer.Handler(newItems);
    /// </example>
    /// <typeparam name="T"></typeparam>
    [Obsolete("Use ExclusiveArrayHandler<T> instead", true)]
    public class ItemsInitializer<T> : IDisposable where T : class
    {
        private readonly Func<T, IDisposable> m_Initializer;
        private readonly CancellationTokenRegistration _cancellationTokenRegistration;
        private Dictionary<T, IDisposable> _items = new Dictionary<T, IDisposable>();
        private bool _disposed = false;

        public event Action<Exception> OnError;
        public event Action<T, IDisposable> OnHandleItem;
        public event Action<T, IDisposable> OnDisposeItem;

        public ItemsInitializer(Func<T, IDisposable> initializer, CancellationToken cancellationToken = default)
        {
            m_Initializer = initializer ?? throw new ArgumentNullException(nameof(initializer));
            _cancellationTokenRegistration = cancellationToken.Register(DisposeAll);
        }

        public void Handler(T[] newItems)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ItemsInitializer<T>));

            var newItemsSet = new HashSet<T>(newItems);
            var oldItemsSet = new HashSet<T>(_items.Keys);

            // Определить удалённые элементы и освободить их
            foreach (var oldItem in oldItemsSet)
            {
                if (!newItemsSet.Contains(oldItem))
                {
                    DisposeItem(oldItem);
                }
            }

            // Инициализировать новые элементы
            foreach (var newItem in newItems)
            {
                if (!_items.ContainsKey(newItem))
                {
                    try
                    {
                        var disposable = m_Initializer(newItem);
                        _items[newItem] = disposable;
                        OnHandleItem?.Invoke(newItem, disposable);
                    }
                    catch (Exception ex)
                    {
                        OnError?.Invoke(ex);
                    }
                }
            }
        }

        private void DisposeItem(T item)
        {
            if (_items.TryGetValue(item, out var disposable))
            {
                disposable?.Dispose();
                _items.Remove(item);
                OnDisposeItem?.Invoke(item, disposable);
            }
        }

        public void DisposeAll()
        {
            foreach (var item in new List<T>(_items.Keys))
            {
                DisposeItem(item);
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                DisposeAll();
                _cancellationTokenRegistration.Dispose();
                _disposed = true;
            }
        }
    }

}