namespace Metamatic
{
    using System;
using System.Collections.Generic;

public class ObservableStackLike<T>
{
    // Храним элементы в порядке добавления.
    private readonly LinkedList<T> m_Items = new LinkedList<T>();
    
    // Базовое значение, возвращаемое, если список пустой.
    private readonly T m_BaseValue;
    
    // Событие, уведомляющее об изменении последнего элемента.
    private event Action<T> LatestChanged;

    // Свойство для доступа к последнему элементу.
    // Если элементов нет, возвращаем _baseValue.
    public T latest => m_Items.Count > 0 ? m_Items.Last.Value : m_BaseValue;
    
    /// <summary>
    /// Конструктор принимает базовое значение (например, -1).
    /// </summary>
    public ObservableStackLike(T baseValue)
    {
        m_BaseValue = baseValue;
    }
    
    /// <summary>
    /// Добавляет элемент в коллекцию и возвращает делегат, 
    /// позволяющий удалить этот элемент.
    /// </summary>
    public DisposableAction Push(T value)
    {
        // Добавляем элемент в конец списка.
        var node = m_Items.AddLast(value);
        // Обновляем подписчиков: теперь Latest = value.
        NotifyLatestChanged();

        // Флаг для предотвращения повторного удаления элемента.
        bool isRemoved = false;

        // Возвращаем делегат для удаления элемента.
        return new DisposableAction(() =>
        {
            if (isRemoved) return;
            m_Items.Remove(node); // Remove возвращает void, поэтому просто вызываем.
            isRemoved = true;
            NotifyLatestChanged();
        });
    }
    
    /// <summary>
    /// Позволяет подписаться на изменения последнего элемента.
    /// Возвращает IDisposable для отписки.
    /// Callback сразу вызывается с текущим значением.
    /// </summary>
    public DisposableAction WatchLatest(Action<T> callback)
    {
        LatestChanged += callback;
        // Вызываем сразу с текущим состоянием.
        callback(latest);
        return new DisposableAction(() => LatestChanged -= callback);
    }
    
    /// <summary>
    /// Уведомляет всех подписчиков актуальным значением Latest.
    /// </summary>
    private void NotifyLatestChanged()
    {
        LatestChanged?.Invoke(latest);
    }
}

}