using System;
using UnityEngine;

namespace Metamatic
{
    public class ObservableDisposable<T>
        where T: struct
    {
        private T m_CachedValue;
        private event Action<T> OnUpdate;
        public DisposableCallback Subscribe(Action<T> handler)
        {
            handler(m_CachedValue);
            OnUpdate += handler;
            var self = this;
            return new DisposableCallback(() =>
            {
                self.OnUpdate -= handler;
            });
        }

        public void SetValue(in T value)
        {
            m_CachedValue = value;
            OnUpdate?.Invoke(value);
        }

        public ObservableDisposable(T defaultValue = default)
        {
            m_CachedValue = defaultValue;
            OnUpdate = null;
        }
    }
}