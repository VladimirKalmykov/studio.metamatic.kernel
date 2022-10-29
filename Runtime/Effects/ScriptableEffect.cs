using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic.Effects
{
    public abstract class ScriptableEffect: ScriptableObject, IEffect
    {
        public abstract string GetEffectName();
    }
    public abstract class ScriptableEffect<TTarget> : ScriptableEffect, IEffect<TTarget>
        where TTarget: Effector
    {
        public abstract UniTask RunEffect(EffectExecutionContext<TTarget> executionContext,
            CancellationToken cancellationToken);
    }
    
    public abstract class ScriptableEffect<TTarget, TProps> : ScriptableEffect<TTarget>, IEffectWithProps<TTarget, TProps>
        where TTarget: Effector
    {
        public abstract UniTask RunEffect(EffectExecutionContext<TTarget, TProps> executionContext,
            CancellationToken cancellationToken);
        
        public void Apply(Behaviour behaviour, TProps props = default)
        {
            var effector = behaviour.GetComponent<TTarget>();
        }
    }
}