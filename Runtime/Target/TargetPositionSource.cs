using UnityEngine;

namespace Metamatic
{
    public abstract class TargetPositionSource : PositionSource
    {
        public Transform target
        {
            get => GetTarget();
            set => SetTarget(value);
        }
        
        protected abstract void SetTarget(Transform target);
        protected abstract Transform GetTarget();
    }
}