using UnityEngine;

namespace Metamatic
{
    public abstract class TargetPositionSource : PositionSource
    {
        public IPositionSource target
        {
            set => SetTarget(value);
        }
        
        public abstract void SetTarget(Transform target);
        public abstract void SetTarget(IPositionSource target);
        public abstract IPositionSource GetTarget();
    }
}