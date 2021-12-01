using System.Threading;
using UnityEngine;

namespace Metamatic.Effects
{
    /// <summary>
    /// The kind of effect which can be applied to the specified point on the object
    /// </summary>
    public abstract class ObjectPointEffect: ObjectEffect
    {
        public readonly Vector3 point;
        public ObjectPointEffect(GameObject target, Vector3 point) : base(target)
        {
            this.point = point;
        }
    }
}