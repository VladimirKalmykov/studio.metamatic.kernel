using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic.Effects
{
    public abstract class ScriptableEffect: ScriptableObject, IEffect
    {
        public abstract string GetEffectName();
        
        public EffectCancellableOperationAwaiter Apply(Behaviour behaviour, object props = default)
        {
            // var effector = behaviour.GetComponent<TTarget>() as Effector<TTarget>;
            // return effector.ApplyEffect(this, props);
            return ApplyWithProps(behaviour, props);
        }
        internal abstract EffectCancellableOperationAwaiter ApplyWithProps(Behaviour behaviour, object props = default);
    }
    public abstract class ScriptableEffect<TTarget> : ScriptableEffect, IEffect<TTarget>
        where TTarget: Behaviour
    {
        /// <summary>
        /// Determines the behaviour of the effect when another same effect applies
        /// </summary>
        public abstract EffectDuplicationBehaviour effectDuplication { get; }
        
        // [Obsolete("Dont' use runEffect with TTarget. Use effect.Apply instead. The reason is passing Effects propsless")]
        // public abstract UniTask RunEffect(EffectExecutionContext<TTarget> executionContext,
        //     CancellationToken cancellationToken);
    }
    
    public abstract class ScriptableEffect<TTarget, TProps> : ScriptableEffect<TTarget>, IEffectWithProps<TTarget, TProps>
        where TTarget: Behaviour
    {
        public abstract UniTask RunEffect(EffectExecutionContext<TTarget, TProps> executionContext,
            CancellationToken cancellationToken);
        
        public EffectCancellableOperationAwaiter Apply(Behaviour behaviour, TProps props = default)
        {
            var effector = behaviour.GetComponent<TTarget>() as Effector<TTarget>;
            return effector.ApplyEffect(this, props);
        }
        /// <summary>
        /// Method used only internal
        /// </summary>
        /// <param name="behaviour"></param>
        /// <param name="props"></param>
        /// <returns></returns>
        internal sealed override EffectCancellableOperationAwaiter ApplyWithProps(Behaviour behaviour, object props = default)
        {
            return Apply(behaviour, (TProps)props);
        }
    }
}