namespace Metamatic
{
    using System;
using System.Collections.Generic;
using System.Threading;

public class ItemsInitializer<T> : IDisposable where T : class
{
    private readonly Func<T, IDisposable> m_Initializer;
    private readonly CancellationTokenRegistration _cancellationTokenRegistration;
    private Dictionary<T, IDisposable> _items = new Dictionary<T, IDisposable>();
    private bool _disposed = false;

    public event Action<Exception> OnError;
    public event Action<T, IDisposable> OnHandleItem;
    public event Action<T, IDisposable> OnDisposeItem;

    public ItemsInitializer(Func<T, IDisposable> initializer, CancellationToken cancellationToken)
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

    private void DisposeAll()
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