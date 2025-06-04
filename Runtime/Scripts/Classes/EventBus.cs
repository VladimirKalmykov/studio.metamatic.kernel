using System;
using System.Collections.Generic;
using System.Linq;

namespace Metamatic
{
    /// <summary>
    /// Event bus for publishing and subscribing to events of type TEvent. (Not tested yet)
    /// </summary>
    /// <typeparam name="TEvent"></typeparam>
    public class EventBus<TEvent>
    {
        private readonly Dictionary<Type, List<Delegate>> m_Subscribers = new Dictionary<Type, List<Delegate>>();
        public DisposableAction SubscribeEvent<T>(Action<T> action)
        {
            Type key = typeof(T);
            if (!m_Subscribers.TryGetValue(key, out var actions))
            {
                actions = new List<Delegate>();
                m_Subscribers[key] = actions;
            }
            actions.Add(action);
            
            return new DisposableAction(() =>
            {
                if (m_Subscribers.TryGetValue(key, out var actionsList))
                {
                    actionsList.Remove(action);
                    if (actionsList.Count == 0)
                    {
                        m_Subscribers.Remove(key);
                    }
                }
            });
        }
        public void PublishEvent<T>(T eventData) where T : TEvent
        {
            Type key = typeof(T);
            if (m_Subscribers.TryGetValue(key, out var actions))
            {
                // Копируем список делегатов в массив. Изменения в `m_Subscribers[key]` далее не затронут эту копию.
                var handlersSnapshot = actions
                    .OfType<Action<TEvent>>()  // явно приводим Delegate → Action<IEvent>
                    .ToArray();
                foreach (var action in handlersSnapshot)
                {
                    try
                    {
                        action(eventData);
                    } catch (Exception ex)
                    {
                        Console.Error("ArcadeLobbyService", $"Error while invoking event handler for {key}: {ex.Message}");
                    }
                }
            }
        }
    }
}