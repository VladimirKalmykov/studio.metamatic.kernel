using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Metamatic
{
    public interface IPin
    {
        /// <summary>
        /// Event triggered when the pin connect to new source pin or disconnect from the source pin
        /// </summary>
        public event Action OnReconnect;
        /// <summary>
        /// Event triggered when the wire becomes disabled or destroyed
        /// </summary>
        public event Action OnUnavailable;
        /// <summary>
        /// Get component that contains this pin (it can be Pin itself or some wrapper component like VirtualPinInput/Output)
        /// </summary>
        /// <returns></returns>
        public UnityEngine.Object GetComponent();

        public Action ConnectInput(IPin pin);
        public void DisconnectOutput();
        public void DisconnectInput();
    }
    
    public interface IPin<T>: IPin
    {
        /// <summary>
        /// Connect another pin to the input of this pin. The data flow will be from the input pin to this pin.
        /// </summary>
        /// <param name="inputPin"></param>
        /// <returns></returns>
        public Action ConnectInput(IPin<T> inputPin);
        public Action ConnectOutput(IPin<T> outputPin);
        public void DisconnectInput(IPin<T> inputPin);
        public void InternalAddOutputPin(IPin<T> pin);
        public void InternalRemoveOutputPin(IPin<T> pin);
        public Wire<T> InternalGetWire();
        public Wire<T> wire { get; }
    }
    public abstract class Pin: MonoBehaviour, IPin, ISerializationCallbackReceiver
    {
        #region debug
        #if DEBUG
        [HideInInspector]
        public bool debug_Inspect;
        public static List<Pin> s_Inspect = new List<Pin>();
        public static event Action<Pin> OnAddToInspect;
        public static event Action<Pin> OnRemoveFromInspect;
        public static void AddToInspect(Pin pin)
        {
            if (pin == null) return;
            s_Inspect.Add(pin);
            pin.debug_Inspect = true;
            OnAddToInspect?.Invoke(pin);
        }

        public static void RemoveFromInspect(Pin pin)
        {
            s_Inspect.Remove(pin);
            pin.debug_Inspect = false;
            OnRemoveFromInspect?.Invoke(pin);
        }
        public static void ToogleInspect(Pin pin)
        {
            if (s_Inspect.Contains(pin))
            {
                RemoveFromInspect(pin);
            }
            else
            {
                AddToInspect(pin);
            }
        }
        public abstract object GetWireObjectData();
        public abstract IPin DebugGetInputPin();
        public abstract IPin[] DebugGetOutputPins();
        public event Action OnDebugDataChanged;
        protected void DebugHandleChangeUntyped()
        {
            OnDebugDataChanged?.Invoke();
        }
        #endif
        #endregion
        public event Action OnReconnect;
        public event Action OnUnavailable;
        public UnityEngine.Object GetComponent()
        {
            return this;
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

        public abstract bool hasInputPin { get; }
        public abstract bool hasOutputPins { get; }
        protected void Reconnect()
        {
            OnReconnect?.Invoke();
        }
        protected void Unavailable()
        {
            OnUnavailable?.Invoke();
        }

        public void OnBeforeSerialize()
        {
            #if DEBUG
            debug_Inspect = s_Inspect.Contains(this);
            #endif
        }

        public void OnAfterDeserialize()
        {
            
        }
    }
    
    public abstract class Pin<T>: Pin, IPin<T>
    {
        #region private
        private Wire<T> m_Wire;
        private IPin<T> m_InputPin;
        private List<IPin<T>> m_OutputPins = new List<IPin<T>>();
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
        #endregion
        
        #region public
        public Wire<T> InternalGetWire()
        {
            return wire;
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
        public Wire<T> wire => m_Wire ??= new Wire<T>();
        
        [Title("Input connected"), ShowIf("@m_InputPin != null"), ShowInInspector, HideLabel, ReadOnly, GUIColor("@m_InputPin != null ? Color.green : Color.red")]
        public IPin<T> inputPin => m_InputPin;
        [Title("Output connected"), ShowIf("@m_OutputPins.Count > 0"),ShowInInspector, HideLabel, ReadOnly, LabelText("Outputs"), GUIColor("@m_OutputPins.Count > 0 ? Color.green : Color.red")]
        [ListDrawerSettings(IsReadOnly = true, NumberOfItemsPerPage = 5, ShowFoldout = true)]
        public IPin<T>[] outputPins => m_OutputPins.ToArray();
        public Action ConnectInput(IPin<T> inputPin)
        {
            if (inputPin == null)
            {
                throw new ArgumentNullException(nameof(inputPin));
            }

            if (inputPin == (IPin<T>)this)
            {
                throw new InvalidOperationException("Can't connect pin to itself");
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
            var anotherWire = m_InputPin.InternalGetWire();
            wire.ConnectInput(anotherWire);
            m_InputPin.OnUnavailable += DisconnectInput;
            Reconnect();

            return () =>
            {
                DisconnectInput(inputPin);
            };
        }

        public Action ConnectOutput(IPin<T> outputPin)
        {
            return outputPin.ConnectInput(this);
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
        public override bool hasInputPin => m_InputPin != null;
        public override bool hasOutputPins => m_OutputPins.Count > 0;
        
        /// <summary>
        /// Create virtual input already connected to this pin
        /// </summary>
        /// <param name="component"></param>
        /// <returns></returns>
        public VirtualPinWriter<T> CreateVirtualInput(Component component)
        {
            return new VirtualPinWriter<T>(component, this);
        }
        /// <summary>
        /// Create virtual output already connected to this pin
        /// </summary>
        /// <param name="component"></param>
        /// <returns></returns>
        public VirtualPinReader<T> CreateVirtualOutput(Component component)
        {
            return new VirtualPinReader<T>(component, this);
        }
        #endregion
        #region protected
        protected sealed override Action InternalConnectInputWithSameType(IPin pin)
        {
            return ConnectInput((IPin<T>)pin);
        }
        #endregion
        
        #region MonoBehaviour
        void OnDestroy()
        {
            Unavailable();
            DisconnectInput();
            wire.Dispose();
        }
        #if DEBUG
        private Action m_DebugUnsubscribeWire;
        void OnEnable()
        {
            m_DebugUnsubscribeWire = wire.SubscribeRef(DebugHandleChangeTyped);
            if (debug_Inspect && !s_Inspect.Contains(this))
            {
                AddToInspect(this);
            }
        }
        void OnDisable()
        {
            m_DebugUnsubscribeWire?.Invoke();
        }
        private void DebugHandleChangeTyped(ref T data)
        {
            DebugHandleChangeUntyped();
        }
        #endif
        #endregion
        
        #region debug
        #if DEBUG
        public override object GetWireObjectData()
        {
            return wire.data;
        }
        public override IPin DebugGetInputPin()
        {
            return inputPin as IPin;
        }
        public override IPin[] DebugGetOutputPins()
        {
            return outputPins.Cast<IPin>().ToArray();
        }
        #endif
        #endregion
    }
}