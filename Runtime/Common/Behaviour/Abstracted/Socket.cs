namespace Metamatic {
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public abstract class Socket : PluggableMonoBehaviour, ISerializationCallbackReceiver
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
        protected T GetParentSocket<T>(bool required = false) where T : Socket {
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

        void Awake() {
            this.OnSocketAwake();
            this.OnSocketAttach();
        }

        void OnTransformParentChanged() {
            this.OnSocketAttach();
        }
    }

}