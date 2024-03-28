using System.Collections.Generic;

namespace Metamatic
{
    using System;
    public class SimplyObservableDictionary<TKey, TValue>: Dictionary<TKey, TValue> where TValue : notnull
    {
        public event Action<TKey, TValue> OnAdd;
        public event Action<TKey, TValue> OnRemove;
        public new void Add(TKey key, TValue value)
        {
            base.Add(key, value);
            OnAdd?.Invoke(key, value);
        }
        public new void Remove(TKey key)
        {
            TValue value = base[key];
            base.Remove(key);
            OnRemove?.Invoke(key, value);
        }
            
        public new TValue this[TKey key]
        {
            get => base[key];
            set
            {
                base[key] = value;
                OnAdd?.Invoke(key, value);
            }
        }
            
        public System.Action SubscribeAdd(System.Action<TKey, TValue> handler)
        {
            OnAdd += handler;
            return () => OnAdd -= handler;
        }
            
        public System.Action SubscribeRemove(System.Action<TKey, TValue> handler)
        {
            OnRemove += handler;
            return () => OnRemove -= handler;
        }
    }
}