using System;
using UnityEngine;

namespace Metamatic.Effects
{
    public class EffectInstruction<TTarget>
        where TTarget: Behaviour
    {
        public readonly IEffect<TTarget> effect;

        public EffectInstruction(IEffect<TTarget> effect)
        {
            if (effect == null)
            {
                throw new ArgumentException("Effect can not be null");
            }
            this.effect = effect;
        }
        
        /*[Obsolete("Run propless effects is obsolete", true)]
        public EffectCancellableOperationAwaiter Run(EffectsChannel<TTarget> channel)
        {
            return OnRun(channel);
        }*/
        
        /*[Obsolete("Run propless effects is obsolete", true)]
        public virtual EffectCancellableOperationAwaiter OnRun(EffectsChannel<TTarget> channel)
        {
            return channel.Add(effect);
        }*/
    }
    
    public class EffectInstruction<TTarget, TProps>: EffectInstruction<TTarget>
        where TTarget: Behaviour
    {
        public readonly  TProps props;

        public EffectInstruction(IEffect<TTarget> effect, TProps props): base(effect)
        {
            this.props = props;
        }
        
        /*public override EffectCancellableOperationAwaiter OnRun(EffectsChannel<TTarget> channel)
        {
            return channel.Add(effect as IEffectWithProps<TTarget, TProps>, props);
        }*/
    }
    
    [Serializable]
    public struct EffectRecipe
    {
        public ScriptableEffect effect;

        public EffectRecipe(ScriptableEffect effect)
        {
            this.effect = effect;
        }
    }
    
    [Serializable]
    public struct EffectRecipe<TProps>
    {
        public ScriptableEffect effect;
        public TProps props;

        public EffectRecipe(ScriptableEffect effect, TProps props)
        {
            this.effect = effect;
            this.props = props;
        }
    }
}