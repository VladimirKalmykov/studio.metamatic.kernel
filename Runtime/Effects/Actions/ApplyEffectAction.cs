using System;
using Codice.Client.GameUI.Explorer;
using Metamatic.Effects;
using UnityEngine;

namespace Metamatic.Actions
{
    public class ApplyEffectAction : Action
    {
        public readonly ScriptableEffect effect;
        public readonly object props;
        public ApplyEffectAction(ScriptableEffect effect, object props, ActionSource source): base(source)
        {
            this.effect = effect;
            this.props = props;
        }

        public override string GetDebugMessage()
        {
            return $"fx:apply({effect.GetEffectName()}) {base.GetDebugMessage()}";
        }
    }
}