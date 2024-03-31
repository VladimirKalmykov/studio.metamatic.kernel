using System;
using Sirenix.OdinInspector;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Metamatic
{
    [Obsolete("Use Socket with global settings instead.", true)]
    public class SocketProvider<TSocketScheme>: Socket<TSocketScheme>, ISerializationCallbackReceiver
    where TSocketScheme: SocketScheme, new()
    {
        private static MicroState<SocketProvider<TSocketScheme>> m_Instance = new();
        public static MicroState<SocketProvider<TSocketScheme>> instance
        {
            get
            {
                return m_Instance;
            }
        }
        private void Awake()
        {
            if (m_Instance.value == null)
            {

                m_Instance.value = this;
            }
            else
            {
                Debug.LogWarning("Multiple providers detected. This is not supported.");
                Object.Destroy(this);
            }
        }

        private void OnDestroy()
        {
            m_Instance.value = null;
        }
    }
}