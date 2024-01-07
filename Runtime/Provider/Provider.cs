using System;
using Sirenix.OdinInspector;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Metamatic
{
    public class Provider<TSocketScheme>: MonoBehaviour, ISerializationCallbackReceiver
    where TSocketScheme: SocketScheme, new()
    {
        private static ObservableWritableValue<Provider<TSocketScheme>> m_Instance = new();
        public static ObservableWritableValue<Provider<TSocketScheme>> instance
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

        [SerializeField][LabelText("Socket")] private Socket<TSocketScheme> m_Socket;
        public Socket<TSocketScheme> socket
        {
            get
            {
                if (m_Socket == null)
                {
                    m_Socket = GetComponent<Socket<TSocketScheme>>();
                }
                return m_Socket;
            }
        }


        public void OnBeforeSerialize()
        {
            if (m_Socket == null)
            {
                m_Socket = GetComponent<Socket<TSocketScheme>>();
            }
        }

        public void OnAfterDeserialize()
        {
            if (m_Socket == null)
            {
                m_Socket = GetComponent<Socket<TSocketScheme>>();
            }
        }
        
        /// <summary>
        /// Connect to plug
        /// </summary>
        /// <param name="plug"></param>
        public void Connect(Plug<TSocketScheme> plug)
        {
            socket.Connect(plug);
        }
    }
}