using UnityEngine;

namespace Metamatic.Framework.Workshop
{
    public interface IVirtualCollision
    {
        public Collider2D collider { get; }
        public Rigidbody2D rigidbody { get; }
        public Collider2D otherCollider { get; }
        public Rigidbody2D otherRigidbody { get; }
        public Vector3 relativeVelocity { get; }

        public int contactCount { get; }
        public ContactPoint2D GetContact(int index);
        public int GetContacts(ContactPoint2D[] contacts);
    }
}