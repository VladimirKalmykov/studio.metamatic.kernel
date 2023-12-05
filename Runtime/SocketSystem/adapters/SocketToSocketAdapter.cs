using Sirenix.OdinInspector;
using UnityEngine;

namespace Metamatic
{
    public abstract class SocketToSocketAdapter<TState, TEventType, TSourceState, TSourceEventTypes>: Socket<TState, TEventType>
        where TState : struct
        where TSourceState: struct
    {
        [SuffixLabel("@GetTargetSuffixLabel(target)", Overlay = false)]
        [GUIColor("GetTargetConnectionColor")]
        [ShowInInspector]
        [SerializeField]
        [PropertyOrder(1)]
        public Socket<TSourceState, TSourceEventTypes> target;

        protected sealed override void OnSocketEnabled()
        {
            target = target != null ? target : GetComponentInParent<Socket<TSourceState, TSourceEventTypes>>();

            if (target == null) {
                enabled = false;
                return;
            }

            OnAdapterEnabled();
        }
        
        protected sealed override void OnSocketDisabled()
        {
            OnAdapterDisabled();
        }

        protected abstract void OnAdapterEnabled();
        protected abstract void OnAdapterDisabled();

#if UNITY_EDITOR
        private Color GetTargetConnectionColor() {
            return target != null ? new Color(0.18f, 0.58f, 0.47f) : Color.gray;
        }
        
        private string GetTargetSuffixLabel(Socket<TSourceState, TSourceEventTypes> target)
        {
            return target != null ? target.name : string.Empty;
        }
#endif
    }
    
    public abstract class SocketToSocketAdapter<TState, TSourceState, TSourceEventTypes>: Socket<TState>
        where TState : struct
        where TSourceState: struct
    {
        [SuffixLabel("@GetTargetSuffixLabel(target)", Overlay = false)]
        [GUIColor("GetTargetConnectionColor")]
        [ShowInInspector]
        [SerializeField]
        [PropertyOrder(1)]
        protected Socket<TSourceState, TSourceEventTypes> target;

        protected sealed override void OnSocketEnabled()
        {
            target = target != null ? target : GetComponentInParent<Socket<TSourceState, TSourceEventTypes>>();

            if (target == null) {
                enabled = false;
                return;
            }

            OnAdapterEnabled();
        }
        
        protected sealed override void OnSocketDisabled()
        {
            OnAdapterDisabled();
        }

        protected abstract void OnAdapterEnabled();
        protected abstract void OnAdapterDisabled();

#if UNITY_EDITOR
        private Color GetTargetConnectionColor() {
            return target != null ? new Color(0.18f, 0.58f, 0.47f) : Color.gray;
        }
        
        private string GetTargetSuffixLabel(Socket<TSourceState, TSourceEventTypes> target)
        {
            return target != null ? target.name : string.Empty;
        }
#endif
    }
}