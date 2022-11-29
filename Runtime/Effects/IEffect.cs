using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic.Effects
{
    public enum EffectDuplicationBehaviour
    {
        Overlay,
        Recreate,
        Update
    }
    public interface IEffect
    {
        public string GetEffectName();
    }
    public interface IEffect<TTarget>: IEffect
        where TTarget: Behaviour
    {
        public EffectDuplicationBehaviour effectDuplication { get; } 
    }

    public interface IEffectWithProps<TEffector, TProps> : IEffect<TEffector>
        where TEffector: Effector
        where TProps: struct
    {
        public UniTask RunEffect(EffectExecutionContext<TEffector, TProps> executionContext, CancellationToken cancellationToken);

        public UniTask InternalRunEffect(EffectExecutionContext<TEffector, TProps> executionContext,
            CancellationToken cancellationToken);
    }
}