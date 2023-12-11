using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Metamatic
{
    public interface IPin<T>
    {
        public Wire<T> wire { get; }
        public Pin<T> inputPin { get; }
        public Pin<T>[] outputPins { get; }
        /// <summary>
        /// Event triggered when the pin connect to new source pin or disconnect from the source pin
        /// </summary>
        public event Action OnReconnect;
        /// <summary>
        /// Event triggered when the wire becomes disabled or destroyed
        /// </summary>
        public event Action OnUnavailable;
        /// <summary>
        /// Connect another pin to the input of this pin. The data flow will be from the input pin to this pin.
        /// </summary>
        /// <param name="inputPin"></param>
        /// <returns></returns>
        public Action ConnectInput(Pin<T> inputPin);
        /// <summary>
        /// Connect another pin to the output of this pin. The data flow will be from this pin to the output pin.
        /// </summary>
        /// <param name="outputPin"></param>
        /// <returns></returns>
        public Action ConnectOutput(Pin<T> outputPin);
        /// <summary>
        /// Disconnect any input pin
        /// </summary>
        public void DisconnectInput();
        /// <summary>
        /// Disconnect input pin if it is connected to the specified pin
        /// </summary>
        /// <param name="inputPin"></param>
        /// <param name="silent"></param>
        public void DisconnectInput(Pin<T> inputPin);
    }
    public abstract class Pin: MonoBehaviour
    {
        
    }
    
    public abstract class Pin<T>: Pin, IPin<T>
    {
        #region private
        private Wire<T> m_Wire;
        private Pin<T> m_InputPin;
        private List<Pin<T>> m_OutputPins = new List<Pin<T>>();
        private void InternalAddOutputPin(Pin<T> outputPin)
        {
           if (outputPin == null) {
                throw new ArgumentNullException(nameof(outputPin));
           }
           if (m_OutputPins.Contains(outputPin)) {
               throw new InvalidOperationException("This output pin already added");
           }
           m_OutputPins.Add(outputPin);
        }
        private void InternalRemoveOutputPin(Pin<T> outputPin)
        {
            if (outputPin == null) {
                throw new ArgumentNullException(nameof(outputPin));
            }
            if (!m_OutputPins.Contains(outputPin)) {
                throw new InvalidOperationException("This output pin is not added");
            }
            m_OutputPins.Remove(outputPin);
        }
        #endregion
        
        #region public
        public Wire<T> wire => m_Wire ??= new Wire<T>();
        public event Action OnReconnect;
        public event Action OnUnavailable;
        [Title("Input connected"), ShowIf("@m_InputPin != null"), ShowInInspector, HideLabel, ReadOnly, GUIColor("@m_InputPin != null ? Color.green : Color.red")]
        public Pin<T> inputPin => m_InputPin;
        [Title("Output connected"), ShowIf("@m_OutputPins.Count > 0"),ShowInInspector, HideLabel, ReadOnly, LabelText("Outputs"), GUIColor("@m_OutputPins.Count > 0 ? Color.green : Color.red")]
        [ListDrawerSettings(IsReadOnly = true, NumberOfItemsPerPage = 5, ShowFoldout = true)]
        public Pin<T>[] outputPins => m_OutputPins.ToArray();
        public Action ConnectInput(Pin<T> inputPin)
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
            m_Wire.ConnectInput(m_InputPin.wire);
            m_InputPin.OnUnavailable += DisconnectInput;

            return () =>
            {
                DisconnectInput(inputPin);
            };
        }

        public Action ConnectOutput(Pin<T> outputPin)
        {
            return outputPin.ConnectInput(this);
        }

        public void DisconnectInput()
        {
            if (m_InputPin == null)
            {
                return;
            }
            DisconnectInput(m_InputPin);
        }

        public void DisconnectInput(Pin<T> inputPin)
        {
            if (inputPin == null)
            {
                throw new ArgumentNullException(nameof(inputPin));
            }
            if (m_InputPin != inputPin)
            {
                return;
            }
            m_Wire.DisconnectInput(inputPin.wire);
            m_InputPin.InternalRemoveOutputPin(this);
            m_InputPin.OnUnavailable -= DisconnectInput;
        }
        #endregion
        
        #region MonoBehaviour
        void Awake()
        {
            m_Wire = new Wire<T>();
        }
        void OnDestroy()
        {
            m_Wire.Dispose();
        }
        #endregion
    }
}