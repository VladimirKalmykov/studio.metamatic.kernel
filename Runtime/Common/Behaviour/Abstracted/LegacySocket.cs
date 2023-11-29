using System;

namespace Metamatic {
    using UnityEngine;

    public abstract class LegacySocket : PluggableMonoBehaviour, ISerializationCallbackReceiver
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
        protected T GetParentSocket<T>(bool required = false) where T : LegacySocket {
            var parentSocket = transform.parent != null ? transform.parent.GetComponentInParent<T>() : null;

            if (required && parentSocket == null) {
                #if UNITY_EDITOR
                    throw new System.Exception("Socket is required");
                #endif
            }

            return parentSocket;
        }

        protected virtual void OnSocketAwake() {}

        protected virtual void OnSocketAttach() {

        }

        private void Awake() {
            this.OnSocketAwake();
            this.OnSocketAttach();
        }

        private void OnTransformParentChanged() {
            this.OnSocketAttach();
        }

        public event Action OnDidUpdate;

        public void DidUpdate()
        {
            
            OnDidUpdate?.Invoke();
        }
    }

}