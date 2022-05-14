using UnityEngine;

namespace Metamatic.Effects
{
    /// <summary>
    /// The kind of effect that can be applied to the point in space
    /// </summary>
    public abstract class SpaceLegacyEffect: LegacyEffect
    {
        public readonly Vector3 position;
        public SpaceLegacyEffect(Vector3 position) : base()
        {
            this.position = position;
        }
    }
}