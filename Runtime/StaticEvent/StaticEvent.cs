namespace Metamatic {
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Static event must be used as static properties to emit global for component type events
    /// </summary>
    /// <example>
    /// PhysocalBodyCollision.CollisionStaticEvent.Emit(collisionData)
    /// </example>
    /// <typeparam name="T"></typeparam>
    public class StaticEvent<T> {
        public List<Action<T>> listeners = new List<Action<T>>();
        public void Emit(T data) {
            foreach (var handler in listeners) {
                handler(data);
            }
        }
        public void AddListener(Action<T> handler) {
            listeners.Add(handler);
        }

        public void RemoveListener(Action<T> handler) {
            listeners.Remove(handler);
        }
    }
}