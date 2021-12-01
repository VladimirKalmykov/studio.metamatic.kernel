using System.Threading;
using UnityEngine;

namespace Metamatic.Effects
{
    /// <summary>
    /// The kind of effect that can be applied to the point in space
    /// </summary>
    public abstract class SpaceEffect: Effect
    {
        public readonly Vector3 position;
        public SpaceEffect(Vector3 position) : base()
        {
            this.position = position;
        }
    }
}