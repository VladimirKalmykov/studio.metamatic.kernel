using Metamatic.Effects;

namespace Metamatic.Actions
{
    public class EffectExitAction : Action
    {
        public readonly ScriptableEffect effect;
        public readonly object outerProps;
        public EffectExitAction(ScriptableEffect effect, object outerProps, ActionSource source): base(source)
        {
            this.effect = effect;
            this.outerProps = outerProps;
        }

        public override string GetDebugMessage()
        {
            return $"fx:exit({effect.GetEffectName()}) {base.GetDebugMessage()}";
        }
    }
}