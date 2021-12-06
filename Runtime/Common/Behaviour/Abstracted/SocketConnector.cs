namespace Metamatic {
    using UnityEngine;
    using Sirenix.OdinInspector;
    
    public class SocketConnector<TargetT, SocketT>: MonoBehaviour
        where SocketT : MonoBehaviour
        where TargetT: MonoBehaviour {

        protected TargetT target;
        [GUIColor("GetSocketConnectionColor")]
        [ShowInInspector, ReadOnly]
        protected SocketT socket;
        [GUIColor("GetSocketConnectionColor")]
        [ShowInInspector, ReadOnly]
        protected GameObject medium;

        private void OnEnable() {
            target = GetComponent<TargetT>();

            if (target == null) {
                enabled = false;
                return;
            }
            
            socket = this.GetParentComponent<SocketT>();
            if (socket == null) {
                /* Add socket forcibly */
                if (transform.parent != null) {
                    /* Search nearest parent element */
                    var parentElement = this.GetParentComponent<Element>() || transform.parent;
                    socket = transform.parent.gameObject.AddComponent<SocketT>();
                } else {
                    return;
                }
            }
            medium = socket.gameObject;
            OnConnectorEnabled();
        }

        private void OnDisable() {
            OnConnectorDisabled();
        }

        protected virtual void OnConnectorEnabled() {}
        protected virtual void OnConnectorDisabled() {}

        #if UNITY_EDITOR
        private Color GetSocketConnectionColor() {
            return socket != null ? new Color(0.18f, 0.58f, 0.47f) : Color.gray;
        }
        #endif
    }
}