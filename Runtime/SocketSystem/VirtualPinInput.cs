using System;
using UnityEngine;

namespace Metamatic
{
    public interface IVirtualPinInput<T>
    {
        /// <summary>
        /// Set data to pin input (it is possible only if the pin has no input pin)
        /// </summary>
        /// <param name="data"></param>
        public void SetData(T data);
        public void SetData(IWire<T>.Transformer transformer);
        public void Mutate(IWire<T>.Modifier modifier);
    }

    public class VirtualPinInput<T>: VirtualPin, IVirtualPinInput<T>, IPin<T>
    {
        private Component m_Component;
        private IPin<T> m_OutputPin;
        private Wire<T> m_Wire;
        public VirtualPinInput(Component component, IPin<T> pin)
        {
            m_Component = component;
            m_Wire = new Wire<T>();
            pin.ConnectInput(this);
        }
        public void SetData(T data)
        {
            m_Wire.SetData(data);
        }

        public void SetData(IWire<T>.Transformer transformer)
        {
            m_Wire.SetData(transformer);
        }

        public void Mutate(IWire<T>.Modifier modifier)
        {
            m_Wire.Mutate(modifier);
        }

        public event Action OnReconnect;
        public event Action OnUnavailable;
        public Component GetComponent()
        {
            return m_Component;
        }

        public Action ConnectInput(IPin<T> inputPin)
        {
            // Do nothing, because this kind of pin can't have input pin
            throw new InvalidOperationException("Virtual pin input can't have input pin");
            return () => { };
        }

        public void InternalAddOutputPin(IPin<T> pin)
        {
            if (m_OutputPin != null)
            {
                throw new InvalidOperationException("Virtual pin input already has output pin");
            }
            m_OutputPin = pin;
        }

        public void InternalRemoveOutputPin(IPin<T> pin)
        {
            if (pin != m_OutputPin)
            {
                throw new InvalidOperationException("This virtual pin is not output pin of this virtual pin");
            }
            m_OutputPin = null;
        }

        public Wire<T> InternalGetWire()
        {
            return m_Wire;
        }

        public Wire<T> wire => m_Wire;

        public void Dispose()
        {
            OnUnavailable?.Invoke();
        }
        
        #if DEBUG
        public Component GetRelatedComponent()
        {
            return m_Component;
        }
        #endif
    }
}