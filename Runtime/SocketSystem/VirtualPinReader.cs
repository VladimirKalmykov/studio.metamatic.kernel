using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Object = UnityEngine.Object;

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
        public DisposableAction Subscribe(IWire<T>.ChangeHandler handler);
        public DisposableAction SubscribeRef(IWire<T>.ChangeRefHandler handler);
    }
    public class VirtualPinReader<T>: VirtualPin<T>, IVirtualPinOutput<T>, IPin<T>
    {
        public T value => wire.data;
        public VirtualPinReader(Object component): base(component)
        {
        }
        public VirtualPinReader(Object component, IPin<T> pin): base(component)
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

        public DisposableAction Subscribe(IWire<T>.ChangeHandler handler)
        {
            var release = wire.Subscribe(handler);
            return new DisposableAction(() => release());
        }

        public DisposableAction SubscribeRef(IWire<T>.ChangeRefHandler handler)
        {
            var release = wire.SubscribeRef(handler);
            return new DisposableAction(() => release());
        }
        
        public UniTask<T> WaitUntil(Func<T, bool> predicate, CancellationToken cancellationToken)
        {
            return wire.WaitUntil(predicate, cancellationToken);
        }
    }
}