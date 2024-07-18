using System;
using Metamatic.Effects;

namespace Metamatic.Actions
{
    [Obsolete("Is out of  concept", true)]
    public class ApplyEffectActionLegacy : ActionLegacy
    {
        public readonly ScriptableEffect effect;
        public readonly object props;
        public ApplyEffectActionLegacy(ScriptableEffect effect, object props, ActionSource source): base(source)
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