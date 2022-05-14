using UnityEngine;

namespace Metamatic.Effects
{
    /// <summary>
    /// The kind of effect which can be applied to the concrete target
    /// </summary>
    public abstract class ObjectLegacyEffect: LegacyEffect
    {
        public readonly GameObject target;
        public ObjectLegacyEffect(GameObject target) : base()
        {
            this.target = target;
        }
    }
}