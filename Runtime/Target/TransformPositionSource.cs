using UnityEngine;

namespace Metamatic
{
    public struct TransformPositionSource: IPositionSource
    {
        private readonly Transform m_Transform;
        public Transform transform => m_Transform;
        private readonly int m_GUID;
        public Vector3 position => m_Transform == null ? Vector3.zero : m_Transform.position;

        public TransformPositionSource(Transform transform)
        {
            m_Transform = transform;
            m_GUID = transform == null ? 0 : transform.GetInstanceID();
        }
        
        public Vector3 forward => m_Transform == null ? Vector3.zero : m_Transform.forward;
        
        public Vector3 right => m_Transform == null ? Vector3.zero : m_Transform.right;
        
        public Vector3 up => m_Transform == null ? Vector3.zero : m_Transform.up;
        
        
        public override bool Equals(object obj)
        {
            if (obj is TransformPositionSource other)
            {
                return m_GUID == other.m_GUID;
            }
            return false;
        }
        
        /* Support convert to transform */
        public static implicit operator Transform(TransformPositionSource source)
        {
            return source.transform;
        }
    }
}