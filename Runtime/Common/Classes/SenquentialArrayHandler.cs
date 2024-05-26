namespace Metamatic
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
        
    /// <summary>
    /// Exclusive handler for array of items. It allows to initialize (or some way process it) items and dispose them when they are not needed anymore.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SequentialArrayHandler<T> : IDisposable where T : class
    {
        public delegate void Handler(T value, CancellationToken cancellationToken);
        private readonly Handler m_Initializer;
        private readonly CancellationTokenRegistration _cancellationTokenRegistration;
        private Dictionary<T, CancellationTokenSource> _items = new Dictionary<T, CancellationTokenSource>();
        private bool _disposed = false;

        public event Action<Exception> OnError;

        public SequentialArrayHandler(Handler initializer, CancellationToken cancellationToken = default)
        {
            m_Initializer = initializer;
            _cancellationTokenRegistration = cancellationToken.Register(CancelAll);
        }

        public void Handle(IEnumerable<T> newItems, Handler localInitializer = null)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SequentialArrayHandler<T>));

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
            
            var initializer = localInitializer ?? m_Initializer;
            
            #if DEBUG
            if (m_Initializer == null)
                throw new ArgumentNullException(nameof(m_Initializer), "Initializer is not set");
            #endif

            // Инициализировать новые элементы
            foreach (var newItem in newItems)
            {
                if (!_items.ContainsKey(newItem))
                {
                    try
                    {
                        var cancellationTokenSource = new CancellationTokenSource();
                        initializer(newItem, cancellationTokenSource.Token);
                        _items[newItem] = cancellationTokenSource;
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
            if (_items.TryGetValue(item, out var cancellationTokenSource))
            {
                cancellationTokenSource.Cancel();
                _items.Remove(item);
            }
        }

        public void CancelAll()
        {
            foreach (var item in new List<T>(_items.Keys))
            {
                DisposeItem(item);
            }
        }
        
        public void Reset()
        {
            CancelAll();
            _items.Clear();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                CancelAll();
                _cancellationTokenRegistration.Dispose();
                _disposed = true;
            }
        }
    }

}