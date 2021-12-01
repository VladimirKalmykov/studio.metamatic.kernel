using System.Threading;
using UnityEngine;

namespace Metamatic.Effects
{
    /// <summary>
    /// The kind of effect which can be applied to the concrete target
    /// </summary>
    public abstract class ObjectEffect: Effect
    {
        public readonly GameObject target;
        public ObjectEffect(GameObject target) : base()
        {
            this.target = target;
        }
    }
}