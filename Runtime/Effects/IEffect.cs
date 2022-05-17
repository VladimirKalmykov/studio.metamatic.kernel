using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic.Effects
{
    public interface IEffect
    {
        public string GetEffectName();
    }
    public interface IEffect<TTarget>: IEffect
        where TTarget: MonoBehaviour
    {
        public UniTask RunEffect(EffectExecutionContext<TTarget> executionContext, CancellationToken cancellationToken);
    }

    public interface IEffectWithProps<TTarget, TProps> : IEffect<TTarget>
        where TTarget: MonoBehaviour
    {
        public UniTask RunEffect(EffectExecutionContext<TTarget, TProps> executionContext, CancellationToken cancellationToken);
    }
}