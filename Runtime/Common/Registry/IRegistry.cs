namespace Metamatic {
    using System;

    public interface IRegistry<T> {
        event Action<T> OnAdd;
        event Action<T> OnRemove;
        void Add(T item);
        void Remove(T item);
        Action Subscribe(Action<T> addHandler, Action<T> removeHandler);
        void Unsubscribe(Action<T> handler, Action<T> removeHandler);
    }
}