using System;
using System.Collections.Generic;
using UnityEngine;

namespace Metamatic
{
    public abstract class VirtualPin: IPin
    {
        protected UnityEngine.Object component;
        #if DEBUG
        private Color m_DebugColor;
        public Color debugColor => m_DebugColor == default(Color) ? (m_DebugColor = DebugOnly.Utils.DebugGetRandomSaturatedColor()) : m_DebugColor;
        #endif
        public event Action OnReconnect;
        public event Action OnUnavailable;
        public UnityEngine.Object GetComponent()
        {
            return component;
        }

        public Action ConnectInput(IPin pin)
        {
            if (pin.GetType() != GetType())
            {
                throw new InvalidOperationException("Can't connect pins of different types");
            }

            return InternalConnectInputWithSameType(pin);
        }

        public abstract void DisconnectOutput();

        public abstract void DisconnectInput();

        protected abstract Action InternalConnectInputWithSameType(IPin pin);

        protected void Reconnect()
        {
            OnReconnect?.Invoke();
        }

        protected void Unavailable()
        {
            OnUnavailable?.Invoke();
        }
    }
    
    
    public class VirtualPin<T>: VirtualPin, IPin<T>
    {
        private IPin<T> m_InputPin;
        private List<IPin<T>> m_OutputPins = new List<IPin<T>>();
        private Wire<T> m_Wire;
        public IPin<T>[] outputPins => m_OutputPins.ToArray();
        public Wire<T> wire => m_Wire ??= new Wire<T>();

        public VirtualPin(UnityEngine.Object component)
        {
            base.component = component;
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
            if (inputPin == (IPin<T>)this)
            {
                throw new InvalidOperationException("Can't connect pin to itself");
            }
            /* Connect input pin to this pin */
            m_InputPin = inputPin;
            /* Add this pin to output pins of the input pin */
            m_InputPin.InternalAddOutputPin(this);
            /* Connect wires */
            wire.ConnectInput(m_InputPin.InternalGetWire());
            m_InputPin.OnUnavailable += DisconnectInput;

            return () =>
            {
                DisconnectInput(inputPin);
            };
        }

        public sealed override void DisconnectOutput()
        {
            foreach (var outputPin in outputPins)
            {
                outputPin.DisconnectInput(this);
            }
        }

        public sealed override void DisconnectInput()
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
            wire.DisconnectInput(inputPin.InternalGetWire());
            m_InputPin.InternalRemoveOutputPin(this);
            m_InputPin.OnUnavailable -= DisconnectInput;
            m_InputPin = null;
            Reconnect();
        }
        
        public Action ConnectOutput(IPin<T> outputPin)
        {
            return outputPin.ConnectInput(this);
        }


        public void InternalAddOutputPin(IPin<T> outputPin)
        {
            if (outputPin == null) {
                throw new ArgumentNullException(nameof(outputPin));
            }
            if (m_OutputPins.Contains(outputPin)) {
                throw new InvalidOperationException("This output pin already added");
            }
            m_OutputPins.Add(outputPin);
            Reconnect();
        }

        public void InternalRemoveOutputPin(IPin<T> outputPin)
        {
            if (outputPin == null) {
                throw new ArgumentNullException(nameof(outputPin));
            }
            if (!m_OutputPins.Contains(outputPin)) {
                throw new InvalidOperationException("This output pin is not added");
            }
            m_OutputPins.Remove(outputPin);
            Reconnect();
        }

        public Wire<T> InternalGetWire()
        {
            return wire;
        }

        public void Dispose()
        {
            Unavailable();
            DisconnectInput();
            wire.Dispose();
        }
        
        #region protected
        protected sealed override Action InternalConnectInputWithSameType(IPin pin)
        {
            return ConnectInput((IPin<T>)pin);
        }
        #endregion
        
        #if DEBUG
        public UnityEngine.Object GetRelatedComponent()
        {
            return component;
        }
        #endif
    }
}