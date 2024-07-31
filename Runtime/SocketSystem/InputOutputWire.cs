using System;
using System.Collections.Generic;
using Metamatic;

namespace Metamatic
{
    public abstract class InputModernWire<TOutputWire> : ModernWire, IInputModernWire<TOutputWire>
        where TOutputWire : IOutputModernWire
    {
        private readonly List<TOutputWire> m_OutputWires = new();
        
        public DisposableAction ConnectIn(TOutputWire outputWire)
        {
            #if DEBUG
            if (m_OutputWires.Contains(outputWire))
            {
                throw new InvalidOperationException("This output wire already connected");
            }
            #endif
            ((IInputModernWire)this).InternalConnectOutput(outputWire);

            return new DisposableAction(() =>
            {
                ((IInputModernWire)this).InternalDisconnectOutput(outputWire);
                outputWire.InternalDisconnectInput(this);
            });
        }

        public bool IsConnectedIn(TOutputWire outputWire)
        {
            return m_OutputWires.Contains(outputWire);
        }

        protected abstract void OnOutputWireConnected(TOutputWire outputWire);
        protected abstract void OnOutputWireDisconnected(TOutputWire outputWire);

        void IInputModernWire.InternalConnectOutput(IOutputModernWire outputWire)
        {
            #if DEBUG
            if (outputWire == null)
            {
                throw new ArgumentNullException(nameof(outputWire));
            }
            #endif
            if (m_OutputWires.Contains((TOutputWire)outputWire))
            {
                return;
            }
            if (connectionType != IModernWire.ConnectionType.Multiple)
            {
                if (connectionType == IModernWire.ConnectionType.SingleError && m_OutputWires.Count > 0)
                {
                    throw new InvalidOperationException("This output wire already connected");
                }
                foreach (var wire in m_OutputWires.ToArray())
                {
                    ((IInputModernWire)this).InternalDisconnectOutput(wire);
                }
            }
            var typed = (TOutputWire)outputWire;
            m_OutputWires.Add((TOutputWire)outputWire);
            OnOutputWireConnected(typed);
            outputWire.InternalConnectInput(this);
        }

        void IInputModernWire.InternalDisconnectOutput(IOutputModernWire outputWire)
        {
            var typed = (TOutputWire)outputWire;
            if (m_OutputWires.Contains(typed))
            {
                m_OutputWires.Remove(typed);
                if (typed.IsConnectedOut(this))
                {
                    typed.InternalDisconnectInput(this);
                }
                OnOutputWireDisconnected(typed);
            }
        }

        public bool IsConnectedIn(IOutputModernWire outputWire)
        {
            return m_OutputWires.Contains((TOutputWire)outputWire);
        }
    }

    public abstract class OutputModernWire<TInputWire> : ModernWire, IOutputModernWire<TInputWire>
        where TInputWire : IInputModernWire
    {
        private List<TInputWire> m_InputWires = new();

        public DisposableAction ConnectOut(TInputWire inputWire)
        {
            #if DEBUG
            if (m_InputWires.Contains((TInputWire)inputWire))
            {
                throw new InvalidOperationException("This input wire already connected");
            }
            #endif
            ((IOutputModernWire)this).InternalConnectInput(inputWire);

            return new DisposableAction(() =>
            {
                ((IOutputModernWire)this).InternalDisconnectInput(inputWire);
            });
        }

        protected abstract void OnInputWireConnected(TInputWire inputWire);
        protected abstract void OnInputWireDisconnected(TInputWire inputWire);

        void IOutputModernWire.InternalConnectInput(IInputModernWire inputWire)
        {
            #if DEBUG
            if (inputWire == null)
            {
                throw new ArgumentNullException(nameof(inputWire));
            }
            #endif
            if (m_InputWires.Contains((TInputWire)inputWire))
            {
                return;
            }
            if (connectionType != IModernWire.ConnectionType.Multiple)
            {
                if (connectionType == IModernWire.ConnectionType.SingleError && m_InputWires.Count > 0)
                {
                    throw new InvalidOperationException("This input wire already connected");
                }
                foreach (var wire in m_InputWires.ToArray())
                {
                    ((IOutputModernWire)this).InternalDisconnectInput(wire);
                }
            }

            var typed = (TInputWire)inputWire;
            m_InputWires.Add(typed);
            OnInputWireConnected(typed);
            inputWire.InternalConnectOutput(this);
        }

        void IOutputModernWire.InternalDisconnectInput(IInputModernWire inputWire)
        {
            var typed = (TInputWire)inputWire;
            if (m_InputWires.Contains(typed))
            {
                m_InputWires.Remove(typed);
                if (typed.IsConnectedIn(this))
                {
                    typed.InternalDisconnectOutput(this);
                }
                OnInputWireDisconnected(typed);
            }
        }

        public bool IsConnectedOut(IInputModernWire inputWire)
        {
            return m_InputWires.Contains((TInputWire)inputWire);
        }
    }
}