using System;
using System.Collections.Generic;
using UnityEngine;

namespace Metamatic
{
    public interface IWire
    {
        
    }
    public interface IWire<T> : IWire
    {
        public delegate void ChangeHandler(T data);
        public delegate void ChangeRefHandler(ref T data);
        public delegate T Transformer(ref T state);
        public delegate void Modifier(ref T state);
        public Wire<T> inputWire { get; }
        public Wire<T>[] outputWires { get; }
        public Wire<T> sourceWire { get; }
        /// <summary>
        /// Current memoized wire data
        /// </summary>
        public T data { get; }
        /// <summary>
        /// Current memoized wire data reference (for working with structs)
        /// </summary>
        /// <returns></returns>
        public ref T GetDataRef();
        /// <summary>
        /// Connect another wire to the input of this wire. The data flow will be from the input wire to this wire.
        /// </summary>
        /// <param name="wire"></param>
        /// <returns>Disconnector</returns>
        public Action ConnectInput(IWire<T> wire);
        /// <summary>
        /// Connect another wire to the output of this wire. The data flow will be from this wire to the output wire.
        /// </summary>
        /// <param name="wire"></param>
        /// <returns>Disconnector</returns>
        public Action ConnectOutput(IWire<T> wire);
        /// <summary>
        /// Disconnect any input wire
        /// </summary>
        public void DisconnectInput(bool silent = false);
        /// <summary>
        /// Disconnect input wire if it is connected to the specified wire
        /// </summary>
        /// <param name="wire"></param>
        public void DisconnectInput(IWire<T> wire, bool silent = false);
        /// <summary>
        /// Set data
        /// </summary>
        /// <param name="data"></param>
        /// <param name="silent">Don't trigger change event (output wires won't know about changes)</param>
        public void SetData(T data, bool silent = false);

        public void SetData(Transformer transformer, bool silent = false);
        /// <summary>
        /// Set data
        /// </summary>
        /// <param name="modifier"></param>
        /// <param name="silent">Don't trigger change event (output wires won't know about changes)</param>
        public void Mutate(Modifier modifier, bool silent = false);

        /// <summary>
        /// Push changes to the output wires if wire is dirty
        /// </summary>
        public void PushChanges();

        public Action Subscribe(ChangeHandler handler);
        public Action SubscribeRef(ChangeRefHandler handler);
        public void Dispose();
    }
    public class Wire: IWire
    {
        
    }
    public class Wire<T> : Wire, IWire<T>
    {
        #region private
        /// <summary>
        /// Connected wire to the input
        /// </summary>
        private Wire<T> m_InputWire;

        /// <summary>
        /// Connected wires to the output
        /// </summary>
        private List<Wire<T>> m_OutputWires = new List<Wire<T>>();
        private Wire<T> m_SourceWire;
        private T m_Data;
        private bool m_IsDisposed;
        private event Action OnDispose;
        private event IWire<T>.ChangeRefHandler OnChange;
        /// <summary>
        /// Determines that data was changed, but change event did not be invoked
        /// </summary>
        private bool isDirty;
        private void ReconnectInput()
        {
            if (m_InputWire == null) return;
            ConnectInput(m_InputWire);
        }
        private void InternalSetOutputWire(Wire<T> wire)
        {
            m_OutputWires.Add(wire);
        }
        private void InternalRemoveOutputWire(Wire<T> wire)
        {
            m_OutputWires.Remove(wire);
        }
        #endregion
        
        #region public
        public Wire<T> inputWire => m_InputWire;
        public Wire<T>[] outputWires => m_OutputWires.ToArray();
        public Wire<T> sourceWire => m_SourceWire;
        /// <summary>
        /// The event is invoked when the input wire is connected or disconnected
        /// </summary>
        public event Action OnReconnect;
        public Wire() : base()
        {
            
        }
        public Wire(T data) : base()
        {
            m_Data = data;
        }
        ~Wire()
        {
            Dispose();
        }
        public T data
        {
            get
            {
                return (m_SourceWire ?? this).m_Data;
            }
        }

        public ref T GetDataRef()
        {
            return ref (m_SourceWire ?? this).m_Data;
        }

        public Action ConnectInput(IWire<T> wire)
        {
            /* Check for null */
            if (wire == null) throw new ArgumentNullException(nameof(wire));
            /* Check for self connection */
            if (wire == this) throw new ArgumentException("Cannot connect wire to itself");
            /* Check for already connected wire */
            if (m_InputWire != null)
            {
                DisconnectInput(true);
            }
            /* Connect input wire */
            m_InputWire = (Wire<T>) wire;
            /* Connect input wire output */
            m_InputWire.InternalSetOutputWire(this);
            /* Set source wire */
            m_SourceWire = m_InputWire.m_SourceWire ?? m_InputWire;
            /* Subscribe to input wire reconnect event */
            m_InputWire.OnReconnect += ReconnectInput;
            m_InputWire.OnDispose += DisconnectInput;
            /* Call reconnect event */
            OnReconnect?.Invoke();
            /* Call change event (because source changes so data too) */
            OnChange?.Invoke(ref GetDataRef());
            return () =>
            {
                DisconnectInput(wire);
            };
        }

        public Action ConnectOutput(IWire<T> wire)
        {
            return wire.ConnectInput(this);
        }

        public void DisconnectInput()
        {
            DisconnectInput(false);
        }

        public void DisconnectInput(bool silent = false)
        {
            if (m_InputWire == null) return;
            /* Set source wire to null */
            m_SourceWire = null;
            /* Unsubscribe from input wire reconnect event */
            m_InputWire.OnReconnect -= ReconnectInput;
            m_InputWire.OnDispose -= DisconnectInput;
            m_InputWire.InternalRemoveOutputWire(this);
            /* Set input wire to null */
            m_InputWire = null;
            if (!silent)
            {
                OnReconnect?.Invoke();
            }
        }
        
        public void DisconnectInput(IWire<T> wire, bool silent = false)
        {
            if (m_InputWire == wire) DisconnectInput(silent);
        }

        public void SetData(T nextData, bool silent = false)
        {
            if (m_InputWire != null)
            {
                Debug.LogWarning("You cannot change wire data which has connected input");
                return;
            }
            m_Data = nextData;
            if (silent)
            {
                isDirty = true;
            }
            else
            {
                OnChange?.Invoke(ref m_Data);
            }
        }

        public void SetData(IWire<T>.Transformer transformer, bool silent = false)
        {
            if (m_InputWire != null)
            {
                Debug.LogWarning("You cannot change wire data which has connected input");
                return;
            }
            m_Data = transformer(ref m_Data);
            if (silent)
            {
                isDirty = true;
            }
            else
            {
                OnChange?.Invoke(ref m_Data);
            }
        }

        public void Mutate(IWire<T>.Modifier modifier, bool silent = false)
        {
            #if DEBUG
            if (m_InputWire != null) Debug.LogWarning("You cannot change wire data which has connected input");
            #endif
            modifier(ref m_Data);
            if (silent)
            {
                isDirty = true;
            }
            else
            {
                OnChange?.Invoke(ref m_Data);
            }
        }

        public void PushChanges()
        {
            if (isDirty)
            {
                OnChange?.Invoke(ref m_Data);
            }
        }

        public Action Subscribe(IWire<T>.ChangeHandler handler)
        {
            var sourceWire = m_SourceWire ?? this;
            IWire<T>.ChangeRefHandler handlerRef = (ref T data) =>
            {
                handler(data);
            };
            sourceWire.OnChange += handlerRef;
            Action unsubscribe = null;
            Action reconnect = () =>
            {
                unsubscribe();
                unsubscribe = Subscribe(handler);
            };
            unsubscribe = () =>
            {
                sourceWire.OnChange -= handlerRef;
                OnReconnect -= reconnect;
                OnDispose -= unsubscribe;
                unsubscribe = () => { };
            };
            OnReconnect += reconnect;
            OnDispose += unsubscribe;
            ref var currentData = ref GetDataRef();
            handler(currentData);

            return () =>
            {
                unsubscribe();
            };
        }
        
        public Action SubscribeRef(IWire<T>.ChangeRefHandler handler)
        {
            var sourceWire = m_SourceWire ?? this;
            sourceWire.OnChange += handler;
            Action unsubscribe = null;
            Action reconnect = () =>
            {
                unsubscribe();
                unsubscribe = SubscribeRef(handler);
            };
            unsubscribe = () =>
            {
                sourceWire.OnChange -= handler;
                OnReconnect -= reconnect;
                OnDispose -= unsubscribe;
                unsubscribe = () => { };
            };
            OnReconnect += reconnect;
            OnDispose += unsubscribe;
            ref var currentData = ref GetDataRef();
            handler(ref currentData);

            return () =>
            {
                unsubscribe();
            };
        }

        public void Dispose()
        {
            if (m_IsDisposed) return;
            m_IsDisposed = true;
            OnDispose?.Invoke();
        }

        #endregion
    }
}