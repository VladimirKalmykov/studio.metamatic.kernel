using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic.Effects
{
    public class EffectOperationContext: IDisposable
    {
        public readonly CancellationTokenSource cancellationTokenSource;
        public UniTask task;

        public EffectOperationContext()
        {
            this.cancellationTokenSource = new CancellationTokenSource();
        }

        public void Dispose()
        {
            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();
        }
    }
    public class EffectOperationContext<TTarget>: EffectOperationContext
        where TTarget: MonoBehaviour
    {
        public readonly Effector<TTarget> effector;

        public EffectOperationContext(Effector<TTarget> effector): base()
        {
            this.effector = effector;
        }
    }
    public class EffectExecutionContext<TTarget, TEffectProps>: EffectOperationContext<TTarget>
        where TTarget: MonoBehaviour
    {
        public readonly IEffectWithProps<TTarget, TEffectProps> effect;
        public TEffectProps props;

        public EffectExecutionContext(Effector<TTarget> effector, IEffectWithProps<TTarget, TEffectProps> effect, ref TEffectProps props): base(effector)
        {
            this.effect = effect;
            this.props = props;
            effector.effectExecutions.Add(this);
            task = effect.RunEffect(this, this.cancellationTokenSource.Token);
            
            if (task.Status != UniTaskStatus.Pending)
            {
                Release();
                return;
            }

            AsyncUtils.AwaitCallback(task, Release);
        }

        public void Release()
        {
            effector.effectExecutions.Remove(this);
        }

        public void Dispose()
        {
            this.cancellationTokenSource.Cancel();
        }
        
        public EffectCancellableOperationAwaiter GetAwaiter()
        {
            return new EffectCancellableOperationAwaiter(this);
        }
    }
}