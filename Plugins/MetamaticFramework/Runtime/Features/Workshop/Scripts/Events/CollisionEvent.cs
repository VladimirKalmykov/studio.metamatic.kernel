using UnityEngine;

namespace Metamatic.Framework.Workshop
{
    public readonly struct CollisionEvent: IVirtualCollision
    {
        private readonly Collision2D m_Collision;
        
        public CollisionEvent(Collision2D collision)
        {
            m_Collision = collision;
        }
        
        public Collider2D collider => m_Collision.collider;
        public Rigidbody2D rigidbody => m_Collision.rigidbody;
        public Collider2D otherCollider => m_Collision.otherCollider;
        public Rigidbody2D otherRigidbody => m_Collision.otherRigidbody;
        public int contactCount => m_Collision.contactCount;
        public ContactPoint2D GetContact(int index) => m_Collision.GetContact(index);
        public int GetContacts(ContactPoint2D[] contacts) => m_Collision.GetContacts(contacts);
        public Vector3 relativeVelocity => m_Collision.relativeVelocity;
    }
}