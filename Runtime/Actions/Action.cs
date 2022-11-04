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
}