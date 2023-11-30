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
        public event Action<MicroSocket<TState>> OnRootSourceSocketChange;
        public event Action OnDestroy;
        private MicroSocket<TState> parentSocket { get; set; }
        /**
         * Root source socket is the socket that has no parent socket.
         */
        private MicroSocket<TState> rootSourceSocket { get; set; }
        /*
         * Fallback state is used when there is no root source socket.
         */
        private static TState _fallbackState;
        public TState state
        {
            get
            {
                return GetStateFromRootParent();
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
         * Connect another socket to this socket. Another socket will read from this socket.
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
            var state = m_StateFactory != null ? m_StateFactory() : GetStateFromRootParent();
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
            if (this.parentSocket != parentSocket)
            {
                /* Unsubscribe from previous parent socket */
                if (this.parentSocket != null)
                {
                    this.parentSocket.OnRootSourceSocketChange -= ApplyRootSourceSocket;
                }
            }
            this.parentSocket = parentSocket;
            /* Calculate root source socket */
            var currentRootSourceSocket = rootSourceSocket;
            rootSourceSocket = this;
            while (rootSourceSocket.parentSocket != null)
            {
                rootSourceSocket = rootSourceSocket.parentSocket;
            }
            if (currentRootSourceSocket != rootSourceSocket)
            {
                OnRootSourceSocketChange?.Invoke(rootSourceSocket);
            }
            /* Subscribe to parent socket root source socket change */
            parentSocket.OnRootSourceSocketChange += ApplyRootSourceSocket;
        }
        
        void ApplyRootSourceSocket(MicroSocket<TState> rootSourceSocket)
        {
            this.rootSourceSocket = rootSourceSocket;
            OnRootSourceSocketChange?.Invoke(rootSourceSocket);
        }
        
        /**
         * Unsubscribe from this socket.
         * @param parentSocket parent socket to unsubscribe
         * @throws Exception if socket is not a parent socket
         */
        public void UnsetParentSocket(MicroSocket<TState> parentSocket)
        {
            if (this.parentSocket == parentSocket)
            {
                SetParentSocket(null);
            }
            else
            {
                throw new Exception("Socket is not a parent socket");
            }
        }

        private ref TState GetStateFromRootParent()
        {
            return ref _fallbackState;
        }
    }
}