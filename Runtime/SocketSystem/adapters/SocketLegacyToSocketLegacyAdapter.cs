using Sirenix.OdinInspector;
using UnityEngine;

namespace Metamatic
{
    public abstract class SocketLegacyToSocketLegacyAdapter<TState, TEventType, TSourceState, TSourceEventTypes>: SocketLegacy<TState, TEventType>
        where TState : struct
        where TSourceState: struct
    {
        [SuffixLabel("@GetTargetSuffixLabel(target)", Overlay = false)]
        [GUIColor("GetTargetConnectionColor")]
        [ShowInInspector]
        [SerializeField]
        [PropertyOrder(1)]
        public SocketLegacy<TSourceState, TSourceEventTypes> target;

        protected sealed override void OnSocketEnabled()
        {
            target = target != null ? target : GetComponentInParent<SocketLegacy<TSourceState, TSourceEventTypes>>();

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
        
        private string GetTargetSuffixLabel(SocketLegacy<TSourceState, TSourceEventTypes> target)
        {
            return target != null ? target.name : string.Empty;
        }
#endif
    }
    
    public abstract class SocketLegacyToSocketLegacyAdapter<TState, TSourceState, TSourceEventTypes>: SocketLegacy<TState>
        where TState : struct
        where TSourceState: struct
    {
        [SuffixLabel("@GetTargetSuffixLabel(target)", Overlay = false)]
        [GUIColor("GetTargetConnectionColor")]
        [ShowInInspector]
        [SerializeField]
        [PropertyOrder(1)]
        protected SocketLegacy<TSourceState, TSourceEventTypes> target;

        protected sealed override void OnSocketEnabled()
        {
            target = target != null ? target : GetComponentInParent<SocketLegacy<TSourceState, TSourceEventTypes>>();

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
        
        private string GetTargetSuffixLabel(SocketLegacy<TSourceState, TSourceEventTypes> target)
        {
            return target != null ? target.name : string.Empty;
        }
#endif
    }
}