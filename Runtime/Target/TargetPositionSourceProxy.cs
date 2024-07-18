using UnityEngine;
using UnityEngine.Serialization;

namespace Metamatic
{
    public class TargetPositionSourceProxy: TargetPositionSource
    {
        [FormerlySerializedAs("source")] public TargetPositionSource defaultSource;
        private IPositionSource m_Source;

        private void Start()
        {
            m_Source = defaultSource;
        }
        protected override Vector3 GetPosition()
        {
            return m_Source != null ? m_Source.position : defaultSource.position;
        }

        public override void SetTarget(Transform target)
        {
            m_Source = new TransformPositionSource(target);
        }

        public override void SetTarget(IPositionSource target)
        {
            m_Source = target;
        }

        public override IPositionSource GetTarget()
        {
            return m_Source;
        }
    }
}