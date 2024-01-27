using UnityEngine;

namespace Metamatic.Actions
{
    public abstract class ActionLegacy: ActionSource
    {
        public readonly float createdTime;

        public ActionLegacy(ActionSource source) : base(source.GetDebugMessage())
        {
            this.createdTime = Time.time;
        }
    }
    
    public abstract class BehaviourActionLegacy<T>: ActionLegacy
    where T: Behaviour
    {
        public readonly Behaviour behaviour;
        public BehaviourActionLegacy(T behaviour) : base(new ActionSource(behaviour))
        {
            this.behaviour = behaviour;
        }
    }
}