using System;

namespace Metamatic {
    using UnityEngine;

    public abstract class Socket<TState> : PluggableMonoBehaviour, ISerializationCallbackReceiver
    {
        // /// <summary>
        // /// When children connector tries to get parent socket this socket will be skipped and used the same parent instrad
        // /// </summary>
        // public bool isProxy = false;
        
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

        private void Awake() {
            OnSocketAwake();
            OnSocketAttach();
        }
        
        private void OnEnable() {
            OnSocketEnabled();
        }
        
        private void OnDisable() {
            OnSocketDisabled();
        }

        private void OnTransformParentChanged() {
            this.OnSocketAttach();
        }

        public event Action OnDidUpdate;

        public void DidUpdate()
        {
            
            OnDidUpdate?.Invoke();
        }

        public StateMachine<TState> stateMachine = new StateMachine<TState>();
        public ref TState GetState()
        {
            return ref stateMachine.state;
        }
        
        public void SetState(StateMachine<TState>.StateModifier modifier) {
            stateMachine.SetState(modifier);
        }
    }

}