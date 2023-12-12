using System;
using UnityEngine;

namespace Metamatic
{
    public interface IVirtualPinOutput<T>
    {
        /// <summary>
        /// Get data from pin output
        /// </summary>
        /// <returns></returns>
        public T GetData();
        /// <summary>
        /// Get data from pin output by reference (for working with structs)
        /// </summary>
        /// <returns></returns>
        public ref T GetDataRef();
        public Action Subscribe(IWire<T>.ChangeHandler handler);
        public Action SubscribeRef(IWire<T>.ChangeRefHandler handler);
    }
    public class VirtualPinOutput<T>: VirtualPin, IVirtualPinOutput<T>, IPin<T>
    {
        private Component m_Component;
        private IPin<T> m_InputPin;
        private Wire<T> m_Wire;
        public VirtualPinOutput(Component component, IPin<T> pin)
        {
            m_Component = component;
            m_Wire = new Wire<T>();
            ConnectInput(pin);
        }
        public T GetData()
        {
            return m_Wire.data;
        }

        public ref T GetDataRef()
        {
            return ref m_Wire.GetDataRef();
        }

        public Action Subscribe(IWire<T>.ChangeHandler handler)
        {
            return m_Wire.Subscribe(handler);
        }

        public Action SubscribeRef(IWire<T>.ChangeRefHandler handler)
        {
            return m_Wire.SubscribeRef(handler);
        }

        public event Action OnReconnect;
        public event Action OnUnavailable;
        public Component GetComponent()
        {
            return m_Component;
        }

        public Action ConnectInput(IPin<T> inputPin)
        {
            if (inputPin == null)
            {
                throw new ArgumentNullException(nameof(inputPin));
            }
            if (m_InputPin != null)
            {
                DisconnectInput();
            }
            /* Connect input pin to this pin */
            m_InputPin = inputPin;
            /* Add this pin to output pins of the input pin */
            m_InputPin.InternalAddOutputPin(this);
            /* Connect wires */
            m_Wire.ConnectInput(m_InputPin.InternalGetWire());
            m_InputPin.OnUnavailable += DisconnectInput;

            return () =>
            {
                DisconnectInput(inputPin);
            };
        }
        
        public void DisconnectInput()
        {
            if (m_InputPin == null)
            {
                return;
            }
            DisconnectInput(m_InputPin);
        }

        public void DisconnectInput(IPin<T> inputPin)
        {
            if (inputPin == null)
            {
                throw new ArgumentNullException(nameof(inputPin));
            }
            if (m_InputPin != inputPin)
            {
                return;
            }
            m_Wire.DisconnectInput(inputPin.InternalGetWire());
            m_InputPin.InternalRemoveOutputPin(this);
            m_InputPin.OnUnavailable -= DisconnectInput;
            m_InputPin = null;
            OnReconnect?.Invoke();
        }

        public void InternalAddOutputPin(IPin<T> pin)
        {
            throw new Exception("Virtual pin output can't have output pin");
        }

        public void InternalRemoveOutputPin(IPin<T> pin)
        {
            throw new Exception("Virtual pin output can't have output pin");
        }

        public Wire<T> InternalGetWire()
        {
            return m_Wire;
        }

        public Wire<T> wire => m_Wire;

        public void Dispose()
        {
            DisconnectInput();
        }
        
        #if DEBUG
        public Component GetRelatedComponent()
        {
            return m_Component;
        }
        #endif
    }
}