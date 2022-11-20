using System;

namespace Metamatic
{
    public class ObservableValue<TValue>
    {
        private TValue m_Value;
        public event Action<TValue> onChange;
        private Action m_PluginUnsubscribe;

        public TValue value
        {
            get
            {
                return m_Value;
            }
        }

        public ObservableValue(TValue initialValue = default)
        {
            m_Value = initialValue;
        }
        
        public ObservableValue(ObservableValue<TValue> other)
        {
            m_Value = default;
            PlugIn(other);
        }

        public Action Subscribe(Action<TValue> handler)
        {
            handler(m_Value);
            onChange += handler;
            return () =>
            {
                onChange -= handler;
            };
        }
        
        /// <summary>
        /// Set value. This action will automatically unsubscribe plugin
        /// </summary>
        /// <param name="value"></param>
        public void SetValue(TValue value)
        {
            var previousValue = m_Value;
            m_Value = value;
            m_PluginUnsubscribe?.Invoke();
            onChange.Invoke(value);
        }
        
        /// <summary>
        /// Plug another observable value to get its value every time it changed
        /// </summary>
        /// <param name="other"></param>
        public void PlugIn(ObservableValue<TValue> other)
        {
            m_PluginUnsubscribe?.Invoke();
            m_PluginUnsubscribe = other.Subscribe(SetValue);
        }

        public void Unplug()
        {
            m_PluginUnsubscribe?.Invoke();
        }
    }
}