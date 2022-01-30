using UnityEngine;

namespace Metamatic
{
    public abstract class WorldAction : InstantEvent
    {
        public Behaviour initiator
        {
            get;
            private set;
        }
        protected void Invoke(Behaviour initiator)
        {
            this.initiator = initiator;
            base.Emit();
        }
    }
}