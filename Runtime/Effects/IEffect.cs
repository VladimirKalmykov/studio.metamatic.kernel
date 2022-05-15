using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic.Effects
{
    public interface IEffect
    {
        
    }
    public interface IEffect<TContext>: IEffect
        where TContext: MonoBehaviour
    {
    }

    public interface IEffectWithProps<TTarget, TProps> : IEffect<TTarget>
        where TTarget: MonoBehaviour
    {
        public UniTask RunEffect(EffectExecutionContext<TTarget, TProps> executionContext, CancellationToken cancellationToken);
    }
}