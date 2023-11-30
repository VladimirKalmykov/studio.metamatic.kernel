using Sirenix.OdinInspector;
using UnityEngine;

namespace Metamatic
{
    public class SocketToSocketAdapter<TState, TTarget>: Socket<TState>
        where TState : struct
        where TTarget: Socket<TState>
    {
        [SuffixLabel("@GetTargetSuffixLabel(target)", Overlay = false)]
        [GUIColor("GetTargetConnectionColor")]
        [ShowInInspector]
        [SerializeField]
        [PropertyOrder(1)]
        protected TTarget target;

        protected sealed override void OnSocketEnabled()
        {
            target = target != null ? target : GetParentComponent<TTarget>();

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

        protected virtual void OnAdapterEnabled()
        {
        }
        protected virtual void OnAdapterDisabled()
        {
        }

#if UNITY_EDITOR
        private Color GetTargetConnectionColor() {
            return target != null ? new Color(0.18f, 0.58f, 0.47f) : Color.gray;
        }
        
        private string GetTargetSuffixLabel(TTarget target)
        {
            return target != null ? target.name : string.Empty;
        }
#endif
    }
}