using UnityEngine;

namespace Metamatic.Actions
{
    public abstract class Action
    {
        public abstract string type { get; }
        public readonly Behaviour sourcerer;

        public Action(Behaviour sourcerer)
        {
            this.sourcerer = sourcerer;
        }
    }
}