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
    public class VirtualPinReader<T>: VirtualPin<T>, IVirtualPinOutput<T>, IPin<T>
    {
        private Component m_Component;
        public VirtualPinReader(Component component): base(component)
        {
        }
        public VirtualPinReader(Component component, IPin<T> pin): base(component)
        {
            ConnectInput(pin);
        }
        public T GetData()
        {
            return wire.data;
        }

        public ref T GetDataRef()
        {
            return ref wire.GetDataRef();
        }

        public Action Subscribe(IWire<T>.ChangeHandler handler)
        {
            return wire.Subscribe(handler);
        }

        public Action SubscribeRef(IWire<T>.ChangeRefHandler handler)
        {
            return wire.SubscribeRef(handler);
        }
    }
}