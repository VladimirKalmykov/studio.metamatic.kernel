using System;
using Sirenix.OdinInspector;

namespace Metamatic {
    using UnityEngine;
    
    public enum SocketUnavailableReason
    {
        ParentSocketDisabled,
        ParentSocketDestroyed
    }
    
    public interface ISocket<TState>
    {
        public ref TState GetState();
        public void SetState(StateMachine<TState>.StateModifier modifier);
        public Action SubscribeState(StateMachine<TState>.StateChangeHandler handler);
    }

    public abstract class Socket<TState, TEventType> : PluggableMonoBehaviour, ISerializationCallbackReceiver, ISocket<TState>
    where TState: struct
    {
        private event Action OnReattach;
        private event Action OnDetach;
        private event Action<SocketUnavailableReason> OnUnavailable;
        protected StateMachine<TState> stateMachine = new StateMachine<TState>();
        protected EventMachine<TEventType> eventMachine = new EventMachine<TEventType>();
        [GUIColor("GetProxyConnectionColor")]
        [ShowInInspector, ReadOnly, LabelText("Proxy")]
        private Socket<TState, TEventType> m_ParentSocket;
        
#if UNITY_EDITOR
        private Color GetProxyConnectionColor() {
            return m_ParentSocket != null ? new Color(0.18f, 0.58f, 0.47f) : Color.gray;
        }
        
#endif
        
        private void Awake() {
            OnSocketAwake();
            OnSocketAttach();
        }
        
        private void OnEnable() {
            OnSocketEnabled();
        }
        
        private void OnDisable() {
            OnUnavailable?.Invoke(SocketUnavailableReason.ParentSocketDisabled);
            OnSocketDisabled();
        }

        private void OnDestroy()
        {
            OnUnavailable?.Invoke(SocketUnavailableReason.ParentSocketDestroyed);
        }

        private void OnTransformParentChanged() {
            this.OnSocketAttach();
        }
        public void OnAfterDeserialize() {
            this.OnAfterSocketDeserialize();
        }

        public void OnBeforeSerialize() {
            
        }

        protected virtual void OnAfterSocketDeserialize() {

        }
        protected T GetParentComponent<T>(bool required = false) where T : MonoBehaviour {
            var parent = transform.parent != null ? transform.parent.GetComponentInParent<T>() : null;

            if (required && parent == null) {
                #if UNITY_EDITOR
                    throw new System.Exception("Parent is required");
                #endif
            }

            return parent;
        }

        protected virtual void OnSocketAwake() {}

        protected virtual void OnSocketAttach() {

        }
        
        protected virtual void OnSocketEnabled() {}
        protected virtual void OnSocketDisabled() {}
        /**
         * Attach socket to parent socket
         */
        public Action AttachSocket(Socket<TState, TEventType> socket)
        {
            if (socket == null) {
                throw new System.Exception("Socket is required");
            }
            m_ParentSocket = socket;
            stateMachine = socket.stateMachine;
            eventMachine = socket.eventMachine;
            OnReattach?.Invoke();
            /* Subscribe to parent socket reattach event */
            socket.OnReattach += ReattachToParent;
            socket.OnUnavailable += UnattachFromParent;
            return () => {
                DetachSocket(socket);
            };
        }
        
        public void ReattachToParent()
        {
            if (m_ParentSocket != null)
            {
                DetachSocket(m_ParentSocket);
                AttachSocket(m_ParentSocket);
            }
        }
        
        public void UnattachFromParent(SocketUnavailableReason reason)
        {
            DetachSocket(m_ParentSocket);
        }
        
        public void DetachSocket(Socket<TState, TEventType> socket)
        {
            if (m_ParentSocket == socket)
            {
                /* Unsubscribe from parent socket reattach and unavailable events */
                socket.OnReattach -= ReattachToParent;
                socket.OnUnavailable -= UnattachFromParent;
                /* Reset state machine to parent state machine and detach from parent */
                var lastParentState = stateMachine.state;
                stateMachine = new StateMachine<TState>(lastParentState);
                eventMachine = new EventMachine<TEventType>();
                m_ParentSocket = null;
                OnReattach?.Invoke();
            }
        }

        
        public ref TState GetState()
        {
            return ref stateMachine.state;
        }
        
        public void SetState(StateMachine<TState>.StateModifier modifier) {
            stateMachine.SetState(modifier);
        }
        
        public Action SubscribeState(StateMachine<TState>.StateChangeHandler handler)
        {
            var currentStateMachine = stateMachine;
            var unsubscribe = currentStateMachine.SubscribeState(handler);
            Action reattachHandler = () => {
                unsubscribe();
                unsubscribe = stateMachine.SubscribeState(handler);
            };
            OnReattach += reattachHandler;
            return () =>
            {
                OnReattach -= reattachHandler;
                unsubscribe();
            };
        }
        
        public Action SubscribeEvent(EventMachine<TEventType>.EventDelegate<TEventType> handler)
        {
            var unsubscribe = eventMachine.Subscribe(handler);
            Action reattachHandler = () => {
                unsubscribe();
                unsubscribe = eventMachine.Subscribe(handler);
            };
            OnReattach += reattachHandler;
            return () =>
            {
                OnReattach -= reattachHandler;
                unsubscribe();
            };
        }
        
        public void Emit(TEventType args)
        {
            eventMachine.Emit(args);
        }
    }
    
    public class Socket<TState> : Socket<TState, bool>
    where TState: struct
    {
        
    }

}