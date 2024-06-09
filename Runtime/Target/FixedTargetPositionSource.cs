using Sirenix.OdinInspector;
using UnityEngine;

namespace Metamatic
{
    public class FixedTargetPositionSource : TargetPositionSource, ISerializationCallbackReceiver
    {
        [SerializeField, HideInPlayMode] private Transform defaultTarget;
        [ShowInInspector, Sirenix.OdinInspector.ReadOnly, HideInEditorMode]
        private IPositionSource m_Target;
        protected override Vector3 GetPosition()
        {
            return m_Target == null ? Vector3.zero : m_Target.position;
        }

        public override void SetTarget(Transform target)
        {
            m_Target = new TransformPositionSource(target);
        }

        public override void SetTarget(IPositionSource target)
        {
            m_Target = target;
        }

        public override IPositionSource GetTarget()
        {
            return m_Target;
        }

        public void OnBeforeSerialize()
        {
            
        }

        public void OnAfterDeserialize()
        {
            target = new TransformPositionSource(defaultTarget);
        }
    }
}