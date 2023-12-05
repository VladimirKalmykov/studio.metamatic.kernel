using UnityEngine;

namespace Metamatic.Actions
{
    public abstract class Action: ActionSource
    {
        public readonly float createdTime;

        public Action(ActionSource source) : base(source.GetDebugMessage())
        {
            this.createdTime = Time.time;
        }
    }
    
    public abstract class BehaviourAction<T>: Action
    where T: Behaviour
    {
        public readonly Behaviour behaviour;
        public BehaviourAction(T behaviour) : base(new ActionSource(behaviour))
        {
            this.behaviour = behaviour;
        }
    }
}