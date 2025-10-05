using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Metamatic.SocketSystem
{
    public abstract class DeviceController: MonoBehaviour
    {
        private SignedIndexedHashPool<(Device device, SignalSource signalSource)> m_ConnectedDevices = new(10);
        
        #if UNITY_EDITOR
        [ShowInInspector, ReadOnly]
        public Device[] connectedDevices => m_ConnectedDevices.ToArray().Select(v => v.device).ToArray();
        #endif

        public DisposableAction RegisterDevice(Device device)
        {
            var registerSignal = SignalSource.Create();
            var token = m_ConnectedDevices.Add((device, registerSignal));
            OnDeviceConnected(device, registerSignal.Signal);
            return new DisposableAction(() =>
            {
                if (m_ConnectedDevices.Remove(token, out var pair))
                {
                    pair.signalSource.Dispose();
                }
            });
        }

        protected abstract void OnDeviceConnected(Device device, Signal token);
    }
}