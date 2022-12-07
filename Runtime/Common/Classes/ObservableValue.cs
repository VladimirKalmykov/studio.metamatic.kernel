using System;
using Metamatic;

namespace Metamatic
{
    interface IObservableValue<TValue>
    {
        public TValue value { get; }
        public Action Subscribe(Action<TValue> handler);
    }
    public abstract class ObservableValue<TValue>: IObservableValue<TValue>
    {
        private event Action<TValue> OnChange;

        public TValue value
        {
            get => GetValue();
            set => SetValue(value);
        }

        public ObservableValue()
        {
            
        }

        public Action Subscribe(Action<TValue> handler)
        {
            handler(GetValue());
            OnChange += handler;
            return () =>
            {
                OnChange -= handler;
            };
        }

        protected void EmitValue(TValue v)
        {
            OnChange?.Invoke(v);
        }

        

        public abstract TValue GetValue();

        public abstract void SetValue(TValue value);
    }

    public class ObservableWritableValue<TValue> : ObservableValue<TValue>
    {
        private TValue m_Value;
        
        public ObservableWritableValue(TValue initialValue = default)
        {
            m_Value = initialValue;
        }

        public override TValue GetValue()
        {
            return m_Value;
        }

        /// <summary>
        /// Set value. This action will automatically unsubscribe plugin
        /// </summary>
        /// <param name="value"></param>
        public override void SetValue(TValue value)
        {
            m_Value = value;
            EmitValue(value);
        }
    }

    public class ObservableGettableValue<TValue> : ObservableValue<TValue>
    {
        private Func<TValue> m_Getter;
        public ObservableGettableValue(Func<TValue> getter) : base()
        {
            m_Getter = getter;
        }
        public override TValue GetValue()
        {
            return m_Getter != null ? m_Getter.Invoke() : default;
        }

        public override void SetValue(TValue value)
        {
            throw new Exception("Set value of ObservableGettableValue is out of concept");
        }

        public void EmitUpdate()
        {
            EmitValue(value);
        }
    }

    public class ObservableProxyValue<TValue> : ObservableValue<TValue>
    {
        protected Action pluginUnsubscribe;
        protected ObservableValue<TValue> pluggedIn;
        public ObservableProxyValue(): base()
        {
            
        }
        public ObservableProxyValue(ObservableValue<TValue> other): base()
        {
            PlugIn(other);
        }
        
        public override TValue GetValue()
        {
            return pluggedIn.value;
        }

        public override void SetValue(TValue value)
        {
            throw new Exception("Unable to set value of proxy value");
        }
        
        /// <summary>
        /// Plug another observable value to get its value every time it changed
        /// </summary>
        /// <param name="other"></param>
        public void PlugIn(ObservableValue<TValue> other)
        {
            pluginUnsubscribe?.Invoke();
            pluggedIn = other;
            pluginUnsubscribe = other.Subscribe(v =>
            {
                EmitValue(v);
            });
            EmitValue(other.value);
        }
        
        public void Unplug()
        {
            if (pluggedIn != null)
            {
                pluggedIn = null;
                pluginUnsubscribe?.Invoke();
            }
        }
    }
}