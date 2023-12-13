using System;
using System.Collections.Generic;
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

    public abstract class SocketLegacy : PluggableMonoBehaviour
    {
        [BoxGroup("Socket"), LabelText("Name")]
        public string socketName;
        /// <summary>
        /// Auto search and connect current socket to the socket with the same type in game object
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="asSource">If true then found socket will be attached to this socket, if false then this socket will be attached to found socket</param>
        public abstract void AutoAttach(GameObject gameObject, bool asSource = false);

        /// <summary>
        /// Detach socket from any attached sockets from game object
        /// </summary>
        /// <param name="gameObject"></param>
        public abstract void DetachFrom(GameObject gameObject);
    }

    public abstract class SocketLegacy<TState, TEventType> : SocketLegacy, ISerializationCallbackReceiver, ISocket<TState>
    where TState: struct
    {
        private event Action OnReattach;
        private event Action OnDetach;
        private event Action<SocketUnavailableReason> OnUnavailable;
        protected StateMachine<TState> stateMachine = new StateMachine<TState>();
        protected EventMachine<TEventType> eventMachine = new EventMachine<TEventType>();
        [BoxGroup("Socket"), GUIColor("GetProxyConnectionColor")]
        [ShowInInspector, ReadOnly, LabelText("Input")]
        [ShowIf("GetProxyVisible")]
        private SocketLegacy<TState, TEventType> m_ParentSocketLegacy;
        [BoxGroup("Socket"), ShowInInspector, LabelText("Output"), ReadOnly]
        private List<SocketLegacy<TState, TEventType>> m_ChildSockets = new List<SocketLegacy<TState, TEventType>>();
        
#if UNITY_EDITOR
        private Color GetProxyConnectionColor() {
            return m_ParentSocketLegacy != null ? new Color(0.18f, 0.58f, 0.47f) : Color.gray;
        }

        private bool GetProxyVisible()
        {
            return m_ParentSocketLegacy != null;
        }
        
#endif
        
        protected virtual TState GetDefaultState() {
            return default;
        }
        
        private void Awake()
        {
            var defaultState = GetDefaultState();
            stateMachine.SetState((ref TState state) =>
            {
                state = defaultState;
            });
            OnSocketAwake();
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
        public Action AttachSocket(SocketLegacy<TState, TEventType> socketLegacy)
        {
            if (socketLegacy == null) {
                throw new System.Exception("Socket is required");
            }
            if (m_ParentSocketLegacy != null) {
                throw new System.Exception("Socket is already attached");
            }
            m_ParentSocketLegacy = socketLegacy;
            stateMachine = socketLegacy.stateMachine;
            eventMachine = socketLegacy.eventMachine;
            socketLegacy.m_ChildSockets.Add(this);
            OnReattach?.Invoke();
            /* Subscribe to parent socket reattach event */
            socketLegacy.OnReattach += ReattachToParent;
            socketLegacy.OnUnavailable += DetachFromParent;
            return () => {
                DetachSocket(socketLegacy);
            };
        }
        
        public override void AutoAttach(GameObject gameObject, bool asSource = false)
        {
            Type currentType = this.GetType();
            var foundSocket = gameObject.GetComponent(currentType) as SocketLegacy<TState, TEventType>;
            if (foundSocket == null)
            {
                Debug.LogWarning("Socket not found");
                return;
            }
            if (asSource)
            {
                foundSocket.AttachSocket(this);
            }
            else
            {
                AttachSocket(foundSocket);
            }
        }

        public override void DetachFrom(GameObject gameObject)
        {
            if (m_ParentSocketLegacy != null && m_ParentSocketLegacy.gameObject == gameObject)
            {
                DetachSocket(m_ParentSocketLegacy);
            }
            if (m_ChildSockets.Count > 0)
            {
                foreach (var childSocket in m_ChildSockets)
                {
                    childSocket.DetachFrom(this.gameObject);
                }
            }
        }
        
        public void ReattachToParent()
        {
            if (m_ParentSocketLegacy != null)
            {
                var cachedParentSocket = m_ParentSocketLegacy;
                DetachSocket(m_ParentSocketLegacy);
                AttachSocket(cachedParentSocket);
            }
        }
        
        public void DetachFromParent(SocketUnavailableReason reason)
        {
            DetachSocket(m_ParentSocketLegacy);
        }
        
        public void DetachSocket(SocketLegacy<TState, TEventType> socketLegacy)
        {
            if (m_ParentSocketLegacy == socketLegacy)
            {
                m_ParentSocketLegacy.m_ChildSockets.Remove(this);
                /* Unsubscribe from parent socket reattach and unavailable events */
                socketLegacy.OnReattach -= ReattachToParent;
                socketLegacy.OnUnavailable -= DetachFromParent;
                /* Reset state machine to parent state machine and detach from parent */
                var lastParentState = stateMachine.prevState;
                stateMachine = new StateMachine<TState>(lastParentState);
                eventMachine = new EventMachine<TEventType>();
                m_ParentSocketLegacy = null;
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
                unsubscribe = this.stateMachine.SubscribeState(handler);
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

        public void ApplyChanges()
        {
            stateMachine.Apply();
        }
        
        public void Emit(TEventType args)
        {
            eventMachine.Emit(args);
        }
    }
    
    public class SocketLegacy<TState> : SocketLegacy<TState, bool>
    where TState: struct
    {
        
    }

}