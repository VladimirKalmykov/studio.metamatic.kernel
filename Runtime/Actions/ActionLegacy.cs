using System;
using UnityEngine;

namespace Metamatic.Actions
{
    [Obsolete("Is out of  concept", true)]
    public abstract class ActionLegacy: ActionSource
    {
        public readonly float createdTime;

        public ActionLegacy(ActionSource source) : base(source.GetDebugMessage())
        {
            this.createdTime = Time.time;
        }
    }
    
    [Obsolete("Is out of  concept", true)]
    public abstract class BehaviourActionLegacy<T>: ActionLegacy
    where T: notnull
    {
        public readonly Behaviour behaviour;
        public BehaviourActionLegacy(T behaviour) : base(new ActionSource())
        {
            this.behaviour = null;
        }
    }
}