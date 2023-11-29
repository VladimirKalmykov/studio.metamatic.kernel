using System;

namespace Metamatic
{
    public class MicroSocket<TState>
        where TState : struct
    {
        public delegate void StateChangeHandler(ref TState nextState);

        public delegate ref TState StateFactoryFunc();
        private StateFactoryFunc m_StateFactory;
        private event StateChangeHandler OnChange;
        public event Action OnDestroy;
        private MicroSocket<TState> m_SourceSocket;
        private TState m_FallbackState;
        public TState writable = default;
        public TState state
        {
            get
            {
                return GetStateFromParent();
            }
        }
        public MicroSocket()
        {
            
        }
        public MicroSocket(StateFactoryFunc stateFactory)
        {
            this.m_StateFactory = stateFactory;
        }
        ~MicroSocket()
        {
            OnDestroy?.Invoke();
        }
        /**
         * Connect another socket to this socket. Another socket will write to this socket.
         */
        public Action Connect(MicroSocket<TState> childSocket)
        {
            childSocket.SetParentSocket(this);
            Action unsubscribe = null;
            OnChange += childSocket.Push;
            unsubscribe = () =>
            {
                childSocket.UnsetParentSocket(this);
                childSocket.OnDestroy -= unsubscribe;
                OnChange -= childSocket.Push;
            };
            childSocket.OnDestroy += unsubscribe;
            return () =>
            {
                
                unsubscribe();
            };
        }
        
        /**
         * Push a new state to this socket
         */
        public void Push(ref TState nextState)
        {
            OnChange?.Invoke(ref nextState);
        }
        
        /**
         * Subscribe to this socket. The handler will be called when the state changes and the state will be passed to the handler immediately.
         * @param handler handler to be called when the state changes
         */
        public Action Subscribe(StateChangeHandler handler)
        {
            var state = m_StateFactory != null ? m_StateFactory() : GetStateFromParent();
            handler(ref state);
            OnChange += handler;
            return () =>
            {
                OnChange -= handler;
            };
        }
        
        /**
         * Unsubscribe from this socket.
         * @param handler parent socket to unsubscribe
         * @return unsubscribe function
         */
        public void SetParentSocket(MicroSocket<TState> parentSocket)
        {
            m_SourceSocket = parentSocket;
        }
        
        /**
         * Unsubscribe from this socket.
         * @param parentSocket parent socket to unsubscribe
         * @throws Exception if socket is not a parent socket
         */
        public void UnsetParentSocket(MicroSocket<TState> parentSocket)
        {
            if (m_SourceSocket == parentSocket)
            {
                m_SourceSocket = null;
            }
            else
            {
                throw new Exception("Socket is not a parent socket");
            }
        }

        private ref TState GetStateFromParent()
        {
            var source = this.m_SourceSocket;
            while (source != null)
            {
                if (source.m_StateFactory != null)
                {
                    return ref source.m_StateFactory();
                }
                source = source.m_SourceSocket;
            }

            return ref m_FallbackState;
        }
    }
}