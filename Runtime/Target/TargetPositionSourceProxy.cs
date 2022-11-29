using UnityEngine;

namespace Metamatic
{
    public class TargetPositionSourceProxy: TargetPositionSource
    {
        public TargetPositionSource source;
        protected override Vector3 GetPosition()
        {
            return source.position;
        }

        protected override void SetTarget(Transform target)
        {
            source.target = target;
        }

        public override Transform GetTarget()
        {
            return source.target;
        }
    }
}