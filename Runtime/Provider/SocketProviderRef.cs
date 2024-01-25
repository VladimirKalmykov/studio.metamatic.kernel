using UnityEngine;

namespace Metamatic
{
    public class SocketProviderRef<TSocketScheme>: AsyncBehaviour
        where TSocketScheme: SocketScheme, new()
    {
        private static ObservableWritableValue<SocketProviderRef<TSocketScheme>> m_Instance = new();
        public static ObservableWritableValue<SocketProviderRef<TSocketScheme>> instance
        {
            get
            {
                return m_Instance;
            }
        }

        public Socket<TSocketScheme> socket;
        private void Start()
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