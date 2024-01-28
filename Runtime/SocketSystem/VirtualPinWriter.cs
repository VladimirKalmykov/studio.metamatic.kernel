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

    public class VirtualPinWriter<T>: VirtualPin<T>, IVirtualPinInput<T>, IPin<T>
    {
        private Component m_Component;

        public VirtualPinWriter(UnityEngine.Object component) : base(component)
        {
            
        }
        public VirtualPinWriter(UnityEngine.Object component, IPin<T> pin): base(component)
        {
            pin.ConnectInput(this);
        }
        public void SetData(T data)
        {
            wire.SetData(data);
        }

        public void Write(T data)
        {
            wire.SetData(data);
        }

        public void SetData(IWire<T>.Transformer transformer)
        {
            wire.SetData(transformer);
        }

        public void Mutate(IWire<T>.Modifier modifier)
        {
            wire.Mutate(modifier);
        }
    }
}