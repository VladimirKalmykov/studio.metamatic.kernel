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

    public interface IEffectWithProps<TTarget, TProps> : IEffect<TTarget>
        where TTarget: Behaviour
    {
        public UniTask RunEffect(EffectExecutionContext<TTarget, TProps> executionContext, CancellationToken cancellationToken);
    }
}